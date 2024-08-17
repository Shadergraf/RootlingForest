using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Manatea.RootlingForest
{
    public class TargetRotationTester : MonoBehaviour
    {
        public Rigidbody Holder;
        public Rigidbody Target;
        public Rigidbody Reference;
        public Transform TargetWorldOrientation;
        public ForceMode forceMode;

        public Vector3 Torque;
        public float TorqueSubstitue;
        public float Mult = 1;
        public float AccelerationMult = 1;

        private ConfigurableJoint joint;
        private Quaternion startRotation;

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                joint = Holder.gameObject.AddComponent<ConfigurableJoint>();

                Holder.detectCollisions = false;

                joint.autoConfigureConnectedAnchor = false;

                joint.xMotion = ConfigurableJointMotion.Limited;
                joint.yMotion = ConfigurableJointMotion.Limited;
                joint.zMotion = ConfigurableJointMotion.Limited;

                joint.linearLimit = new SoftJointLimit() { limit = 0.001f, contactDistance = 0.001f };
                joint.linearLimitSpring = new SoftJointLimitSpring() { spring = 200, damper = 20 };



                joint.angularXDrive = new JointDrive() { useAcceleration = true, positionSpring = 500, positionDamper = 50, maximumForce = 1000 };
                joint.angularYZDrive = new JointDrive() { useAcceleration = true, positionSpring = 500, positionDamper = 50, maximumForce = 1000 };
                joint.rotationDriveMode = RotationDriveMode.XYAndZ;

                joint.configuredInWorldSpace = true;
                Debug.Log(Target.transform.localRotation);
                startRotation = Target.rotation;
                //joint.targetRotation = TargetWorldOrientation.rotation;
                //joint.SetTargetRotation(Quaternion.Inverse(TargetWorldOrientation.rotation), startRotation);

                // Connect!
                joint.connectedBody = Target;

                Holder.detectCollisions = true;
            }

            bool continuous = forceMode == ForceMode.Acceleration || forceMode == ForceMode.Force;
            if ((Input.GetKey(KeyCode.C) && continuous) || (Input.GetKeyDown(KeyCode.C) && !continuous))
            {
                float extraMult = 1;
                if (continuous)
                {
                    extraMult = AccelerationMult;
                }
                Target.AddTorque(Torque * Mult, forceMode);
                Reference.AddForceAtPosition(Vector3.up * TorqueSubstitue * Mult * extraMult, Target.worldCenterOfMass + Vector3.right, forceMode);
                Reference.AddForceAtPosition(-Vector3.up * TorqueSubstitue * Mult * extraMult, Target.worldCenterOfMass - Vector3.right, forceMode);
            }

            if (joint)
            {
                //joint.SetTargetRotation(Quaternion.Inverse(TargetWorldOrientation.rotation), Quaternion.Inverse(startRotation));
                //joint.SetTargetRotation(Quaternion.LookRotation(TargetWorldOrientation.forward, TargetWorldOrientation.up), startRotation);
                joint.targetRotation = TargetWorldOrientation.rotation * Quaternion.Inverse(startRotation);
            }
        }
    }
}
