using Manatea;
using Manatea.GameplaySystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class AttachToContacts : MonoBehaviour
{
    [SerializeField]
    private GameplayAttribute m_StickyAttribute;

    private List<Joint> m_ConnectedJoints = new List<Joint>();
    private Dictionary<Joint, float> m_ConnectedJointMultiplier = new Dictionary<Joint, float>();

    private const float maxBreakForce = 150;
    private const float targetForce = 60;

    private void FixedUpdate()
    {
        for (int i = 0; i < m_ConnectedJoints.Count; i++)
        {
            if (m_ConnectedJoints[i] == null)
            {
                m_ConnectedJoints.RemoveAt(i);
                i--;
                continue;
            }
            Joint joint = m_ConnectedJoints[i];

            joint.breakForce -= joint.currentForce.magnitude * 0.7f * Time.fixedDeltaTime;
            
            if (joint.breakForce >= 100000000)
            {
                joint.breakForce = maxBreakForce;
            }
            else
            {
                joint.breakForce = MMath.Damp(joint.breakForce, targetForce * m_ConnectedJointMultiplier[joint], 0.5f, Time.fixedDeltaTime);
            }
            
            joint.breakTorque = joint.breakForce;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Rigidbody otherRb = collision.collider.attachedRigidbody;
        if (!otherRb)
        {
            return;
        }

        float stickiness = 1;
        if (m_StickyAttribute && otherRb.TryGetComponent(out GameplayAttributeOwner attributes) && attributes.TryGetAttributeEvaluatedValue(m_StickyAttribute, out float stickinessAttr))
        {
            stickiness = stickinessAttr;
        }
        if (stickiness <= 0)
        {
            return;
        }

        // Remove destroyed joints
        m_ConnectedJoints.RemoveAll(j => j == null);

        for (int i = 0; i < m_ConnectedJoints.Count; i++)
        {
            if (m_ConnectedJoints[i] == null)
            {
                m_ConnectedJoints.RemoveAt(i);
                i--;
                continue;
            }
            if (m_ConnectedJoints[i].connectedBody == otherRb)
            {
                return;
            }
        }

        ConfigurableJoint joint = gameObject.AddComponent<ConfigurableJoint>();
        joint.connectedBody = otherRb;

        //Vector3 anchor = joint.connectedAnchor;
        //joint.connectedAnchor = otherRb.transform.InverseTransformPoint(collision.contacts[0].point + Vector3.up * 0.01f);
        //joint.anchor = transform.InverseTransformPoint(collision.contacts[0].point);
        //joint.autoConfigureConnectedAnchor = false;

        joint.xMotion = ConfigurableJointMotion.Locked;
        joint.yMotion = ConfigurableJointMotion.Locked;
        joint.zMotion = ConfigurableJointMotion.Locked;
        joint.angularXMotion = ConfigurableJointMotion.Locked;
        joint.angularYMotion = ConfigurableJointMotion.Locked;
        joint.angularZMotion = ConfigurableJointMotion.Locked;

        //joint.xMotion = ConfigurableJointMotion.Limited;
        //joint.yMotion = ConfigurableJointMotion.Limited;
        //joint.zMotion = ConfigurableJointMotion.Limited;
        //joint.angularXMotion = ConfigurableJointMotion.Limited;
        //joint.angularYMotion = ConfigurableJointMotion.Limited;
        //joint.angularZMotion = ConfigurableJointMotion.Limited;
        //joint.linearLimit = new SoftJointLimit()
        //{
        //    contactDistance = 0.001f,
        //    limit = 0.001f,
        //};
        //joint.linearLimitSpring = new SoftJointLimitSpring()
        //{
        //    damper = 100,
        //    spring = 1000,
        //};

        joint.breakForce    = 100000000;
        joint.breakTorque   = 100000000;

        joint.autoConfigureConnectedAnchor = true;
        Vector3 anchor = joint.connectedAnchor;
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedAnchor = anchor;

        m_ConnectedJoints.Add(joint);


        // TODO limit the distance an object can have to the connected body
        // You can test this on the bounce pad

        m_ConnectedJointMultiplier.Add(joint, stickiness);
    }

    private void OnGUI()
    {
        if (m_ConnectedJoints.Count == 0 || m_ConnectedJoints[0] == null)
            return;

        for (int i = 0; i < m_ConnectedJoints.Count; i++)
        {
            if (m_ConnectedJoints[i])
            {
                MGUI.DrawWorldProgressBar(transform.position + Vector3.up * 0.2f, new Rect(0, i * 5, 20 * m_ConnectedJointMultiplier[m_ConnectedJoints[i]], 4), MMath.InverseLerp(0, maxBreakForce, m_ConnectedJoints[i].breakForce));
            }
        }
    }
}
