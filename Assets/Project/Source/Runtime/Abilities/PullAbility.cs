using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PullAbility : MonoBehaviour
{
    public Rigidbody Target;
    public float JointForce = 500;
    public Vector3 HandPosition = new Vector3(0, .8f, .8f);

    private Joint m_Joint;


    private void OnEnable()
    {
        m_Joint = gameObject.AddComponent<ConfigurableJoint>();
        m_Joint.connectedBody = Target;
        m_Joint.autoConfigureConnectedAnchor = false;
        m_Joint.enableCollision = true;

        // Find closest point
        var colliders = Target.GetComponents<Collider>();
        Vector3 closestPoint = Vector3.one * 10000;
        for (int i  = 0; i < colliders.Length; i++)
        {
            Vector3 newPoint = colliders[i].ClosestPoint(transform.position);
            if (Vector3.Distance(transform.position, newPoint) < Vector3.Distance(transform.position, closestPoint))
                closestPoint = newPoint;
        }
        closestPoint = Target.transform.InverseTransformPoint(closestPoint);

        m_Joint.connectedAnchor = Target.transform.InverseTransformPoint(Target.ClosestPointOnBounds(transform.position));
        m_Joint.anchor = transform.InverseTransformPoint(Target.transform.TransformPoint(m_Joint.connectedAnchor));

        if (m_Joint is SpringJoint)
        {
            (m_Joint as SpringJoint).spring = JointForce;
        }
        if (m_Joint is ConfigurableJoint)
        {
            ConfigurableJoint configurableJoint = (ConfigurableJoint)m_Joint;
            configurableJoint.xMotion = ConfigurableJointMotion.Limited;
            configurableJoint.yMotion = ConfigurableJointMotion.Limited;
            configurableJoint.zMotion = ConfigurableJointMotion.Limited;
            configurableJoint.angularXMotion = ConfigurableJointMotion.Free;
            configurableJoint.angularYMotion = ConfigurableJointMotion.Free;
            configurableJoint.angularZMotion = ConfigurableJointMotion.Free;
        }
    }
    private void OnDisable()
    {
        Destroy(m_Joint);
        m_Joint = null;
    }

    void Update()
    {
        m_Joint.anchor = HandPosition;
    }
}
