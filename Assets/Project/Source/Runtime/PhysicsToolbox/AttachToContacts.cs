using Manatea;
using Manatea.GameplaySystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

// TODO attach to contact breaks when using the bounce pads

public class AttachToContacts : MonoBehaviour
{
    [SerializeField]
    private GameplayAttribute m_StickyAttribute;

    private List<Joint> m_ConnectedJoints = new List<Joint>();
    private Dictionary<Joint, float> m_ConnectedJointMultiplier = new Dictionary<Joint, float>();

    [SerializeField]
    private float maxBreakForce = 150;
    [SerializeField]
    private float targetForce = 60;
    [SerializeField]
    private float randomDisconnectForce = 10;

    private void FixedUpdate()
    {
        for (int i = 0; i < m_ConnectedJoints.Count; i++)
        {
            if (m_ConnectedJoints[i] == null)
            {
                m_ConnectedJoints.RemoveAt(i);
                i--;
                GetComponent<Rigidbody>().AddForce(Random.onUnitSphere * randomDisconnectForce, ForceMode.VelocityChange);
                Debug.Log("NOT connected anymore!");
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

        Rigidbody rigid = GetComponent<Rigidbody>();
        rigid.position = rigid.position + Vector3.up * 0.02f;            // Skin offset to prevent sweeps not registering
        RaycastHit[] hits = rigid.SweepTestAll(-collision.contacts[0].normal, 0.2f);
        rigid.position = rigid.position - Vector3.up * 0.02f;            // Revert skin offset
        if (hits.Length == 0)
        {
            return;
        }
        RaycastHit relevantHit = new();
        bool relevantHitFound = false;
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider.attachedRigidbody == otherRb)
            {
                relevantHit = hits[i];
                relevantHitFound = true;
            }
        }
        if (!relevantHitFound)
        {
            return;
        }

        DebugHelper.DrawWireSphere(collision.contacts[0].point, 0.05f, Color.green);
        DebugHelper.DrawWireSphere(relevantHit.point, 0.05f, Color.blue);

        Vector3 contactOffset = relevantHit.point - collision.contacts[0].point;
        rigid.position += contactOffset;

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

        joint.xMotion = ConfigurableJointMotion.Locked;
        joint.yMotion = ConfigurableJointMotion.Locked;
        joint.zMotion = ConfigurableJointMotion.Locked;
        joint.angularXMotion = ConfigurableJointMotion.Locked;
        joint.angularYMotion = ConfigurableJointMotion.Locked;
        joint.angularZMotion = ConfigurableJointMotion.Locked;


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

        Debug.Log("Connected!");
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
