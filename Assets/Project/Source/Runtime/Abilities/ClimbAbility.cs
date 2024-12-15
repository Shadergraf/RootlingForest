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
    public class ClimbAbility : BaseAbility
    {
        public Rigidbody m_Self;
        public float m_ClimbSpeed = 1;
        public float m_BreakForce = 10;
        public float m_BreakTorque = 10;
        public float m_TestDistance = 0.5f;
        public float m_TestRadius = 0.5f;
        public float m_ClimbingSpeed = 1.5f;
        public float m_ClimbingDistance = 0.41f;
        public float m_LinearDistance = 0.5f;

        [Header("Debug")]
        public bool m_Debug = false;

        private ConfigurableJoint m_Joint;
        private Rigidbody m_GrabSurrogate;

        private RaycastHit[] m_WalHits = new RaycastHit[32];

        public Collider m_ClimbingTarget;
        public Vector3 m_LocalPosition;
        public Vector3 m_LocalForward;

        private Vector3 m_ScheduledMove;
        private Vector3 m_CurrentWallNormal;

        public Vector3 CurrentWallNormal => m_CurrentWallNormal;


        protected override void AbilityEnabled()
        {
            if (!m_GrabSurrogate)
            {
                var gameObj = new GameObject("ClimbTarget");
                gameObj.transform.SetAsFirstSibling();

                m_GrabSurrogate = gameObj.AddComponent<Rigidbody>();
                m_GrabSurrogate.isKinematic = true;
            }

            m_GrabSurrogate.transform.position = m_Self.position;
            m_GrabSurrogate.transform.rotation = m_Self.rotation;
            m_GrabSurrogate.PublishTransform();

            CreateJoint();

            if (DetectWall(m_Self.transform.position, m_Self.transform.forward, out RaycastHit hit))
            {
                Vector3 position = hit.point;
                Vector3 normal = hit.normal.FlattenY().normalized;
                m_ClimbingTarget = hit.collider;

                m_GrabSurrogate.transform.position = m_ClimbingTarget.transform.position;
                m_GrabSurrogate.transform.rotation = m_ClimbingTarget.transform.rotation;
                m_GrabSurrogate.transform.localScale = m_ClimbingTarget.transform.lossyScale;
                m_GrabSurrogate.PublishTransform();

                if (m_ClimbingTarget.attachedRigidbody)
                {
                    m_Joint.connectedBody = m_ClimbingTarget.attachedRigidbody;
                }
                else
                {
                    m_Joint.connectedBody = m_GrabSurrogate;
                }

                m_LocalPosition = m_ClimbingTarget.transform.InverseTransformPoint(position);
                m_LocalForward = m_ClimbingTarget.transform.InverseTransformDirection(-normal);
                m_CurrentWallNormal = normal;

                m_Joint.connectedAnchor = m_LocalPosition;

            }
            else
            {
                enabled = false;
                return;
            }
        }

        protected override void AbilityDisabled()
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

            Vector3 worldPosition = m_ClimbingTarget.transform.TransformPoint(m_LocalPosition);
            Vector3 worldForward = m_ClimbingTarget.transform.TransformDirection(m_LocalForward);
            worldPosition -= worldForward * 0.4f;
            
            Vector3 contactMove = m_ScheduledMove;
            m_ScheduledMove = Vector3.zero;
            worldPosition += contactMove * m_ClimbingSpeed * Time.fixedDeltaTime;

            Debug.Log(m_Joint.currentForce.magnitude);

            //if (DetectWall(worldPosition, worldForward, out RaycastHit hit))
            //{
            //    Vector3 position = hit.point;
            //    Vector3 normal = hit.normal.FlattenY().normalized;
            //    m_ClimbingTarget = hit.collider;
            //
            //    m_GrabSurrogate.transform.position = m_ClimbingTarget.transform.position;
            //    m_GrabSurrogate.transform.rotation = m_ClimbingTarget.transform.rotation;
            //    m_GrabSurrogate.transform.localScale = m_ClimbingTarget.transform.lossyScale;
            //    m_GrabSurrogate.PublishTransform();
            //
            //    if (m_ClimbingTarget.attachedRigidbody)
            //    {
            //        m_Joint.connectedBody = m_ClimbingTarget.attachedRigidbody;
            //    }
            //    else
            //    {
            //        m_Joint.connectedBody = m_GrabSurrogate;
            //    }
            //
            //    m_LocalPosition = m_ClimbingTarget.transform.InverseTransformPoint(position);
            //    m_LocalForward = m_ClimbingTarget.transform.InverseTransformDirection(-normal);
            //    m_CurrentWallNormal = normal;
            //
            //    m_Joint.connectedAnchor = m_LocalPosition;
            //
            //}
            //else
            //{
            //    enabled = false;
            //    return;
            //}
        }

        private void CreateJoint()
        {
            Debug.Assert(!m_Joint, "Joint already present!", this);

            m_Joint = m_Self.gameObject.AddComponent<ConfigurableJoint>();
            m_Joint.connectedBody = m_GrabSurrogate;
            m_Joint.breakForce = m_BreakForce;
            m_Joint.breakTorque = m_BreakTorque;

            m_Joint.xMotion = ConfigurableJointMotion.Locked;
            m_Joint.yMotion = ConfigurableJointMotion.Limited;
            m_Joint.zMotion = ConfigurableJointMotion.Locked;
            m_Joint.angularXMotion = ConfigurableJointMotion.Free;
            m_Joint.angularYMotion = ConfigurableJointMotion.Free;
            m_Joint.angularZMotion = ConfigurableJointMotion.Free;

            m_Joint.linearLimit = new SoftJointLimit() { limit = m_LinearDistance };

            //m_Joint.xDrive = new JointDrive() { maximumForce = m_Joint.xDrive.maximumForce, positionSpring = 1000, positionDamper = 10 };
            //m_Joint.yDrive = new JointDrive() { maximumForce = m_Joint.yDrive.maximumForce, positionSpring = 1000, positionDamper = 10 };
            //m_Joint.zDrive = new JointDrive() { maximumForce = m_Joint.zDrive.maximumForce, positionSpring = 1000, positionDamper = 10 };
            //
            //m_Joint.angularXDrive = new JointDrive() { maximumForce = m_Joint.angularXDrive.maximumForce, positionSpring = 1000, positionDamper = 10 };
            //m_Joint.angularYZDrive = new JointDrive() { maximumForce = m_Joint.angularYZDrive.maximumForce, positionSpring = 1000, positionDamper = 10 };

            m_Joint.anchor = new Vector3(0, 0, m_ClimbingDistance);

            m_Joint.autoConfigureConnectedAnchor = false;
            m_Joint.enableCollision = true;
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
