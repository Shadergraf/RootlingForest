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
    UpdatedBounds = 2,
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

    private ConfigurableJoint m_GrabJoint;
    private Quaternion startRotation;

    private Quaternion cachedRotation;
    private Quaternion targetRotation;
    private Quaternion targetRotDelta;

    public bool CollisionEnabled => m_CollisionEnabled;


    public void PreEstablishGrab(Rigidbody Instigator)
    {
        if (m_Orientations.Length == 0)
        {
            return;
        }

        Vector3 forwardDir = (transform.position - Instigator.transform.position).normalized;
        Vector3 upDir = Instigator.transform.up;
        Vector3 rightDir = Instigator.transform.right;

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
            //rb.rotation = deltaQuat * rb.rotation;
            //rb.PublishTransform();

            //targetRotation = Instigator.rotation;
        }

        cachedRotation = transform.rotation;
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

        switch (m_LocationRule)
        {
            case LocationRule.Center:
                grabJoint.connectedAnchor = Vector3.zero;
                break;
            case LocationRule.Bounds:
                Vector3 handPosWorld = grabJoint.transform.TransformPoint(grabJoint.anchor);
                grabJoint.connectedAnchor = transform.InverseTransformPoint(GetComponent<Rigidbody>().ClosestPointOnBounds(handPosWorld));
                break;
        }

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


        grabJoint.angularXDrive = new JointDrive() { useAcceleration = true, positionSpring = 5000, positionDamper = 500, maximumForce = 10000 };
        grabJoint.angularYZDrive = new JointDrive() { useAcceleration = true, positionSpring = 5000, positionDamper = 500, maximumForce = 10000 };
        grabJoint.rotationDriveMode = RotationDriveMode.XYAndZ;

        startRotation = transform.rotation;
        grabJoint.configuredInWorldSpace = true;
        grabJoint.targetRotation = targetRotation * Quaternion.Inverse(transform.rotation);

        m_GrabJoint = grabJoint;
    }

    public void UpdateGrab()
    {
    }

    public void DisbandGrab(ConfigurableJoint grabJoint)
    {

    }
}
