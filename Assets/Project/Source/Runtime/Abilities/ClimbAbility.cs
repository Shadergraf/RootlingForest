using UnityEngine;
using Manatea;
using Manatea.GameplaySystem;
using UnityEngine.Events;
using System.Collections;
using System;
using UnityEngine.Serialization;
using UnityEngine.Animations;
using UnityEngine.InputSystem.HID;
using UnityEngine.InputSystem;

namespace Manatea.RootlingForest
{
    public class ClimbAbility : MonoBehaviour
    {
        public Rigidbody m_Self;
        public float m_ClimbSpeed = 1;
        public float m_BreakForce = 10;
        public float m_BreakTorque = 10;
        public float m_TestDistance = 0.5f;
        public float m_TestRadius = 0.5f;
        public float m_ClimbingSpeed = 1.5f;
        public float m_ClimbingDistance = 0.41f;

        [Header("Debug")]
        public bool m_Debug = false;

        private ConfigurableJoint m_Joint;
        private Rigidbody m_GrabRigid;

        private RaycastHit[] m_WalHits = new RaycastHit[32];

        public Transform m_ClimbingTarget;
        public Vector3 m_LocalPosition;
        public Vector3 m_LocalForward;

        private Vector3 m_ScheduledMove;
        private Vector3 m_CurrentWallNormal;

        public Vector3 CurrentWallNormal => m_CurrentWallNormal;


        private void OnEnable()
        {
            if (!m_GrabRigid)
            {
                var gameObj = new GameObject("ClimbTarget");
                gameObj.transform.SetAsFirstSibling();

                m_GrabRigid = gameObj.AddComponent<Rigidbody>();
                m_GrabRigid.isKinematic = true;
            }

            m_GrabRigid.transform.position = m_Self.position;
            m_GrabRigid.transform.rotation = m_Self.rotation;
            m_GrabRigid.PublishTransform();

            m_Joint = m_Self.gameObject.AddComponent<ConfigurableJoint>();
            m_Joint.connectedBody = m_GrabRigid;
            m_Joint.autoConfigureConnectedAnchor = false;
            m_Joint.breakForce = m_BreakForce;
            m_Joint.breakTorque = m_BreakTorque;

            m_Joint.xMotion = ConfigurableJointMotion.Free;
            m_Joint.yMotion = ConfigurableJointMotion.Free;
            m_Joint.zMotion = ConfigurableJointMotion.Free;
            m_Joint.angularXMotion = ConfigurableJointMotion.Free;
            m_Joint.angularYMotion = ConfigurableJointMotion.Free;
            m_Joint.angularZMotion = ConfigurableJointMotion.Free;

            m_Joint.xDrive = new JointDrive() { maximumForce = m_Joint.xDrive.maximumForce, positionSpring = 1000, positionDamper = 10 };
            m_Joint.yDrive = new JointDrive() { maximumForce = m_Joint.yDrive.maximumForce, positionSpring = 1000, positionDamper = 10 };
            m_Joint.zDrive = new JointDrive() { maximumForce = m_Joint.zDrive.maximumForce, positionSpring = 1000, positionDamper = 10 };

            m_Joint.angularXDrive = new JointDrive() { maximumForce = m_Joint.angularXDrive.maximumForce, positionSpring = 1000, positionDamper = 10 };
            m_Joint.angularYZDrive = new JointDrive() { maximumForce = m_Joint.angularYZDrive.maximumForce, positionSpring = 1000, positionDamper = 10 };

            m_Joint.anchor = new Vector3(0, 0, m_ClimbingDistance);

            if (DetectWall(m_Self.transform.position, m_Self.transform.forward, out RaycastHit hit))
            {
                Vector3 position = hit.point;
                Vector3 normal = hit.normal.FlattenY().normalized;
                m_ClimbingTarget = hit.collider.transform;
                m_LocalPosition = hit.collider.transform.InverseTransformPoint(position);
                m_LocalForward = hit.collider.transform.InverseTransformDirection(-normal);
                m_GrabRigid.MovePosition(hit.point);
                m_GrabRigid.MoveRotation(Quaternion.LookRotation(-normal));
                m_CurrentWallNormal = normal;
            }
            else
            {
                enabled = false;
                return;
            }
        }

        private void OnDisable()
        {
            if (m_Joint)
            {
                Destroy(m_Joint);
            }
            m_Joint = null;

            m_ClimbingTarget = null;
        }
        private void FixedUpdate()
        {
            if (m_Joint == null || m_Joint.connectedBody == null || m_ClimbingTarget == null)
            {
                enabled = false;
                return;
            }

            Vector3 worldPosition = m_ClimbingTarget.TransformPoint(m_LocalPosition);
            Vector3 worldForward = m_ClimbingTarget.TransformDirection(m_LocalForward);
            worldPosition -= worldForward * 0.4f;

            Vector3 contactMove = m_ScheduledMove;
            m_ScheduledMove = Vector3.zero;
            worldPosition += contactMove * m_ClimbingSpeed * Time.fixedDeltaTime;

            if (DetectWall(worldPosition, worldForward, out RaycastHit hit))
            {
                Vector3 position = hit.point;
                Vector3 normal = hit.normal.FlattenY().normalized;
                m_ClimbingTarget = hit.collider.transform;
                m_LocalPosition = hit.collider.transform.InverseTransformPoint(position);
                m_LocalForward = hit.collider.transform.InverseTransformDirection(-normal);
                m_GrabRigid.MovePosition(hit.point);
                m_GrabRigid.MoveRotation(Quaternion.LookRotation(-normal));
                m_CurrentWallNormal = normal;
            }
            else
            {
                enabled = false;
                return;
            }
        }

        public void Move(Vector3 scheduledMove)
        {
            m_ScheduledMove = scheduledMove;
        }

        private bool DetectWall(Vector3 position, Vector3 forward, out RaycastHit hit)
        {
            hit = new RaycastHit();

            int layerMask = LayerMaskExtensions.CalculatePhysicsLayerMask(gameObject.layer);
            float distance = m_TestDistance;

            Ray detectionRay = new Ray();
            float radius = m_TestRadius;

            detectionRay.origin = position;
            detectionRay.direction = forward;

            int hits = Physics.SphereCastNonAlloc(detectionRay, radius, m_WalHits, distance, layerMask, QueryTriggerInteraction.Ignore);
            if (hits == 0)
                return false;

            hit.distance = float.PositiveInfinity;
            for (int i = 0; i < hits; i++)
            {
                // Discard overlaps
                if (m_WalHits[i].distance == 0)
                    continue;
                // Discard self collisions
                if (m_WalHits[i].collider.transform == m_Self.transform)
                    continue;
                if (m_WalHits[i].collider.transform.IsChildOf(m_Self.transform))
                    continue;
                if (m_WalHits[i].distance > hit.distance)
                    continue;

                hit = m_WalHits[i];
            }

            if (m_Debug)
            {
                DebugHelper.DrawWireCapsule(detectionRay.origin, detectionRay.GetPoint(m_TestDistance), radius, Color.grey);
                DebugHelper.DrawWireCapsule(detectionRay.origin, detectionRay.GetPoint(hit.distance), radius, Color.red);
            }

            // No valid ground found
            if (hit.distance == float.PositiveInfinity || hit.collider == null)
                return false;

            return true;
        }
    }
}
