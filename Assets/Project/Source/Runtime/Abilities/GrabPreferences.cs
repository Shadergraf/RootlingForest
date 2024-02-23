using Manatea;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using static UnityEngine.GraphicsBuffer;

public enum LocationRule
{
    Center = 0,
    Bounds = 1,
    XAxis = 2,
    YAxis = 3,
    ZAxis = 4,
}
public enum RotationRule
{
    Locked = 0,
    Limited = 1,
    Free = 2,
}

public class GrabPreferences : MonoBehaviour
{
    [SerializeField]
    private bool m_CollisionEnabled;
    [SerializeField]
    private LocationRule m_LocationRule;
    [SerializeField]
    private RotationRule m_RotationRule;
    [SerializeField]
    private float m_RotationLimit = 0;
    [SerializeField]
    private bool m_UseOrientations;
    [SerializeField]
    private GrabOrientation[] m_Orientations;
    [SerializeField]
    private bool m_UpdateGrabLocation;
    [SerializeField]
    private bool m_AllowOverlapAfterDrop = true;

    public bool CollisionEnabled => m_CollisionEnabled;
    public LocationRule LocationRule => m_LocationRule;
    public RotationRule RotationRule => m_RotationRule;
    public float RotationLimit => m_RotationLimit;
    public bool UseOrientations => m_UseOrientations;
    public GrabOrientation[] Orientations => m_Orientations;
    public bool UpdateGrabLocation => m_UpdateGrabLocation;
    public bool AllowOverlapAfterDrop => m_AllowOverlapAfterDrop;


    private Quaternion targetRotation;




    public void PreEstablishGrab(Rigidbody Instigator)
    {
        if (m_UseOrientations)
        {
            float bestMatch = float.NegativeInfinity;
            GrabOrientation bestOrientationMatch = null;
            for (int i = 0; i < m_Orientations.Length; i++)
            {
                float match = -Quaternion.Angle(Instigator.transform.rotation, m_Orientations[i].transform.rotation) / MMath.Max(MMath.Epsilon, m_Orientations[i].Weight);
                if (match < bestMatch)
                {
                    continue;
                }
                bestMatch = match;
                bestOrientationMatch = m_Orientations[i];
            }

            if (bestOrientationMatch != null)
            {
                var m = bestOrientationMatch.transform.localToWorldMatrix * transform.worldToLocalMatrix;
                Quaternion deltaQuat = Instigator.transform.rotation * Quaternion.Inverse(bestOrientationMatch.transform.rotation);
                Rigidbody rb = GetComponent<Rigidbody>();
                targetRotation = deltaQuat * rb.rotation;
            }
        }
    }
    public void EstablishGrab(ConfigurableJoint grabJoint)
    {
        grabJoint.enableCollision = m_CollisionEnabled;

        grabJoint.xMotion = ConfigurableJointMotion.Limited;
        grabJoint.yMotion = ConfigurableJointMotion.Limited;
        grabJoint.zMotion = ConfigurableJointMotion.Limited;

        // Rotation rule
        ConfigurableJointMotion angularMotion = ConfigurableJointMotion.Free;
        switch (m_RotationRule)
        {
            case RotationRule.Locked:
                angularMotion = ConfigurableJointMotion.Locked; break;
            case RotationRule.Limited:
                angularMotion = ConfigurableJointMotion.Limited; break;
            case RotationRule.Free:
                angularMotion = ConfigurableJointMotion.Free; break;
        }
        grabJoint.angularXMotion = angularMotion;
        grabJoint.angularYMotion = angularMotion;
        grabJoint.angularZMotion = angularMotion;

        AttachToLocation(grabJoint);

        if (m_RotationRule == RotationRule.Limited)
        {
            SoftJointLimit limit;

            limit = grabJoint.highAngularXLimit;
            limit.limit = m_RotationLimit;
            grabJoint.highAngularXLimit = limit;

            limit = grabJoint.lowAngularXLimit;
            limit.limit = -m_RotationLimit;
            grabJoint.lowAngularXLimit = limit;

            limit = grabJoint.angularYLimit;
            limit.limit = m_RotationLimit;
            grabJoint.angularYLimit = limit;

            limit = grabJoint.angularZLimit;
            limit.limit = m_RotationLimit;
            grabJoint.angularZLimit = limit;
        }

        if (m_UseOrientations)
        {
            grabJoint.angularXDrive = new JointDrive() { useAcceleration = true, positionSpring = 1000000, positionDamper = 50000, maximumForce = 1000000 };
            grabJoint.angularYZDrive = new JointDrive() { useAcceleration = true, positionSpring = 1000000, positionDamper = 50000, maximumForce = 1000000 };
            grabJoint.rotationDriveMode = RotationDriveMode.XYAndZ;

            grabJoint.configuredInWorldSpace = true;
            grabJoint.targetRotation = targetRotation * Quaternion.Inverse(transform.rotation);
        }
    }

    public void UpdateGrab(ConfigurableJoint grabJoint)
    {
        AttachToLocation(grabJoint);


        if (m_UseOrientations)
        {
            Debug.Log(Quaternion.Angle(targetRotation, grabJoint.connectedBody.transform.rotation));
        }
    }

    private void AttachToLocation(ConfigurableJoint grabJoint)
    {
        Vector3 handPosWorld = grabJoint.transform.TransformPoint(grabJoint.anchor);
        switch (m_LocationRule)
        {
            case LocationRule.Center:
                grabJoint.connectedAnchor = Vector3.zero;
                break;
            case LocationRule.Bounds:
                grabJoint.connectedAnchor = transform.InverseTransformPoint(GetComponent<Rigidbody>().ClosestPointOnBounds(handPosWorld));
                break;
            case LocationRule.XAxis:
                grabJoint.connectedAnchor = Vector3.Project(transform.InverseTransformPoint(GetComponent<Rigidbody>().ClosestPointOnBounds(handPosWorld)), Vector3.right);
                break;
            case LocationRule.YAxis:
                grabJoint.connectedAnchor = Vector3.Project(transform.InverseTransformPoint(GetComponent<Rigidbody>().ClosestPointOnBounds(handPosWorld)), Vector3.up);
                break;
            case LocationRule.ZAxis:
                grabJoint.connectedAnchor = Vector3.Project(transform.InverseTransformPoint(GetComponent<Rigidbody>().ClosestPointOnBounds(handPosWorld)), Vector3.forward);
                break;
        }
    }

    public void DisbandGrab(ConfigurableJoint grabJoint)
    {

    }
}
