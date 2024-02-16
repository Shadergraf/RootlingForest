using Manatea;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class AttachToContacts : MonoBehaviour
{
    private List<Joint> m_ConnectedJoints = new List<Joint>();

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
                joint.breakForce = MMath.Damp(joint.breakForce, targetForce, 0.5f, Time.fixedDeltaTime);
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

        joint.xMotion = ConfigurableJointMotion.Limited;
        joint.yMotion = ConfigurableJointMotion.Limited;
        joint.zMotion = ConfigurableJointMotion.Limited;
        joint.angularXMotion = ConfigurableJointMotion.Limited;
        joint.angularYMotion = ConfigurableJointMotion.Limited;
        joint.angularZMotion = ConfigurableJointMotion.Limited;

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

        m_ConnectedJoints.Add(joint);

        // TODO limit the amount of joints that can be spawned here

        // TODO limit the distance an object can have to the connected body
        // You can test this on the bounce pad
    }

    static Texture2D texture;
    private void OnGUI()
    {
        if (m_ConnectedJoints.Count == 0 || m_ConnectedJoints[0] == null)
            return;
        if (texture == null)
        {
            texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
        }

        Rect window = new Rect(50, 50, 300, 30);
        GUI.BeginGroup(window);

        GUI.skin.box.normal.background = texture;

        GUI.backgroundColor = new Color(64, 0, 0);
        GUI.Box(new Rect(0, 0, 300, 50), GUIContent.none);

        GUI.backgroundColor = new Color(0, 128, 0);
        GUI.Box(new Rect(0, 0, MMath.RemapClamped(0, maxBreakForce, 0, 300, m_ConnectedJoints[0].breakForce), 50), GUIContent.none);

        GUI.backgroundColor = new Color(0, 0, 0);
        GUI.Box(new Rect(MMath.RemapClamped(0, maxBreakForce, 0, 300, targetForce) - 1, 0, 20, 50), GUIContent.none);

        GUI.EndGroup();
    }
}
