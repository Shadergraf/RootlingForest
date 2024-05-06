using Manatea;
using Manatea.GameplaySystem;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

// TODO attach to contact breaks when using the bounce pads

public class AttachToContacts : MonoBehaviour
{
    [SerializeField]
    private GameplayAttribute m_StickyAttribute;

    private List<Joint> m_ConnectedJoints = new List<Joint>();
    private Dictionary<Joint, float> m_ConnectedJointMultiplier = new();
    private Dictionary<Rigidbody, float> m_ConnectedCooldown = new();

    [SerializeField]
    [FormerlySerializedAs("maxBreakForce")]
    private float m_MaxBreakForce = 150;
    [SerializeField]
    [FormerlySerializedAs("targetForce")]
    private float m_TargetForce = 60;
    [SerializeField]
    [FormerlySerializedAs("randomDisconnectForce")]
    private float m_RandomDisconnectForce = 10;
    [SerializeField]
    [FormerlySerializedAs("reconnectionDelay")]
    private float m_ReconnectionDelay = 0.2f;
    [SerializeField]
    private LayerMask m_LayerMask = ~0;
    [SerializeField]
    private bool m_OrientToNormal;


    private void FixedUpdate()
    {
        for (int i = 0; i < m_ConnectedJoints.Count; i++)
        {
            if (m_ConnectedJoints[i] == null)
            {
                m_ConnectedJoints.RemoveAt(i);
                i--;
                GetComponent<Rigidbody>().AddForce(Random.onUnitSphere * m_RandomDisconnectForce, ForceMode.VelocityChange);
                //Debug.Log("NOT connected anymore!");

                continue;
            }
            Joint joint = m_ConnectedJoints[i];

            joint.breakForce -= joint.currentForce.magnitude * 0.7f * Time.fixedDeltaTime;
            
            if (joint.breakForce >= 100000000)
            {
                joint.breakForce = m_MaxBreakForce;
            }
            else
            {
                joint.breakForce = MMath.Damp(joint.breakForce, m_TargetForce * m_ConnectedJointMultiplier[joint], 0.5f, Time.fixedDeltaTime);
            }
            joint.breakTorque = joint.breakForce;


            m_ConnectedCooldown[joint.connectedBody] = m_ReconnectionDelay;
        }

        List<Rigidbody> keysToDelete = new List<Rigidbody>();
        var keys = m_ConnectedCooldown.Keys.ToArray();
        foreach (Rigidbody key in keys)
        {
            m_ConnectedCooldown[key] = m_ConnectedCooldown[key] - Time.fixedDeltaTime;
            if (m_ConnectedCooldown[key] <= 0)
            {
                keysToDelete.Add(key);
            }
        }
        for (int i = 0; i < keysToDelete.Count; i++)
        {
            m_ConnectedCooldown.Remove(keysToDelete[i]);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Rigidbody otherRb = collision.collider.attachedRigidbody;
        if (!otherRb)
        {
            otherRb = collision.collider.gameObject.AddComponent<Rigidbody>();
            otherRb.isKinematic = true;
        }
        if (m_ConnectedCooldown.ContainsKey(otherRb))
        {
            return;
        }
        if (!m_LayerMask.ContainsLayer(otherRb.gameObject.layer))
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

        // TODO super hacky, pls do properly
        if (m_OrientToNormal)
        {
            rigid.rotation = Quaternion.LookRotation(relevantHit.normal);
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

        m_ConnectedCooldown.Add(otherRb, m_ReconnectionDelay);

        //Debug.Log("Connected!");
    }

    private void OnGUI()
    {
        if (m_ConnectedJoints.Count == 0 || m_ConnectedJoints[0] == null)
            return;

        for (int i = 0; i < m_ConnectedJoints.Count; i++)
        {
            if (m_ConnectedJoints[i])
            {
                MGUI.DrawWorldProgressBar(transform.position + Vector3.up * 0.2f, new Rect(0, i * 5, 20 * m_ConnectedJointMultiplier[m_ConnectedJoints[i]], 4), MMath.InverseLerp(0, m_MaxBreakForce, m_ConnectedJoints[i].breakForce));
            }
        }
    }
}
