using Manatea.CommandSystem;
using UnityEngine;

namespace Manatea.RootlingForest
{
    public class PhysicsPusher : MonoBehaviour
    {
        [SerializeField]
        private float m_PushForce = 1;
        [SerializeField]
        private Vector3 m_Multiplier = Vector3.one;
        [SerializeField]
        private bool m_Normalized;
        [SerializeField]
        private Collider m_Collider;

        private Rigidbody m_Rigidbody;
        //private CollisionEventSender m_CollisionEventSender;

        private static bool s_DebugForce;


        private void OnTriggerStay(Collider other)
        {
            if (!enabled)
                return;
            if (!other.attachedRigidbody)
                return;

            if (Physics.ComputePenetration(other, other.transform.position, other.transform.rotation, m_Collider, m_Collider.transform.position, m_Collider.transform.rotation, out Vector3 dir, out float dist))
            {
                Vector3 force = dir * m_PushForce;
                if (!m_Normalized)
                    force *= dist;
                force = Vector3.Scale(force, m_Multiplier);
                other.attachedRigidbody.AddForce(force, ForceMode.Force);

                if (s_DebugForce)
                    Debug.DrawLine(other.attachedRigidbody.position, other.attachedRigidbody.position + force);
            }
        }

        [Command]
        private static void DebugPhysicsPusher()
        {
            s_DebugForce = !s_DebugForce;
        }
    }
}
