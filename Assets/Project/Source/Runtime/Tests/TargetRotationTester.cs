using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Manatea.RootlingForest
{
    public class TargetRotationTester : MonoBehaviour
    {
        [FormerlySerializedAs("Holder")]
        [SerializeField]
        private Rigidbody m_Holder;
        [FormerlySerializedAs("Target")]
        [SerializeField]
        private Rigidbody m_Target;
        [FormerlySerializedAs("Reference")]
        [SerializeField]
        private Rigidbody m_Reference;
        [FormerlySerializedAs("TargetWorldOrientation")]
        [SerializeField]
        private Transform m_TargetWorldOrientation;
        [FormerlySerializedAs("forceMode")]
        [SerializeField]
        private ForceMode m_ForceMode;

        [FormerlySerializedAs("Torque")]
        [SerializeField]
        private Vector3 m_Torque;
        [FormerlySerializedAs("TorqueSubstitue")]
        [SerializeField]
        private float m_TorqueSubstitue;
        [FormerlySerializedAs("Mult")]
        [SerializeField]
        private float m_Mult = 1;
        [FormerlySerializedAs("AccelerationMult")]
        [SerializeField]
        private float m_AccelerationMult = 1;

        private ConfigurableJoint m_Joint;
        private Quaternion m_StartRotation;

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                m_Joint = m_Holder.gameObject.AddComponent<ConfigurableJoint>();

                m_Holder.detectCollisions = false;

                m_Joint.autoConfigureConnectedAnchor = false;

                m_Joint.xMotion = ConfigurableJointMotion.Limited;
                m_Joint.yMotion = ConfigurableJointMotion.Limited;
                m_Joint.zMotion = ConfigurableJointMotion.Limited;

                m_Joint.linearLimit = new SoftJointLimit() { limit = 0.001f, contactDistance = 0.001f };
                m_Joint.linearLimitSpring = new SoftJointLimitSpring() { spring = 200, damper = 20 };



                m_Joint.angularXDrive = new JointDrive() { useAcceleration = true, positionSpring = 500, positionDamper = 50, maximumForce = 1000 };
                m_Joint.angularYZDrive = new JointDrive() { useAcceleration = true, positionSpring = 500, positionDamper = 50, maximumForce = 1000 };
                m_Joint.rotationDriveMode = RotationDriveMode.XYAndZ;

                m_Joint.configuredInWorldSpace = true;
                Debug.Log(m_Target.transform.localRotation);
                m_StartRotation = m_Target.rotation;

                // Connect!
                m_Joint.connectedBody = m_Target;

                m_Holder.detectCollisions = true;
            }

            bool continuous = m_ForceMode == ForceMode.Acceleration || m_ForceMode == ForceMode.Force;
            if ((Input.GetKey(KeyCode.C) && continuous) || (Input.GetKeyDown(KeyCode.C) && !continuous))
            {
                float extraMult = 1;
                if (continuous)
                {
                    extraMult = m_AccelerationMult;
                }
                m_Target.AddTorque(m_Torque * m_Mult, m_ForceMode);
                m_Reference.AddForceAtPosition(Vector3.up * m_TorqueSubstitue * m_Mult * extraMult, m_Target.worldCenterOfMass + Vector3.right, m_ForceMode);
                m_Reference.AddForceAtPosition(-Vector3.up * m_TorqueSubstitue * m_Mult * extraMult, m_Target.worldCenterOfMass - Vector3.right, m_ForceMode);
            }

            if (m_Joint)
            {
                m_Joint.targetRotation = m_TargetWorldOrientation.rotation * Quaternion.Inverse(m_StartRotation);
            }
        }
    }
}
