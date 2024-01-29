using System.Collections;
using System.Collections.Generic;
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
    }

    public void DisbandGrab(ConfigurableJoint grabJoint)
    {

    }
}
