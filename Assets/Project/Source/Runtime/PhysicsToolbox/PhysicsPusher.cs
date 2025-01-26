using Manatea.CommandSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Manatea.RootlingForest
{
    [RequireComponent(typeof(Rigidbody))]
    public class PhysicsPusher : MonoBehaviour
    {
        [SerializeField] private float m_PushForce = 1;
        [SerializeField] private Vector3 m_Multiplier = Vector3.one;
        [SerializeField] private bool m_Normalized;
        [SerializeField] private Collider m_Collider;
        [SerializeField] private float m_TickFrequency = 0.5f;
        [EnumFlags]
        [SerializeField] private PushConfig m_PushConfig = PushConfig.Other;

        private Rigidbody m_DetectionRigidbody;
        private Rigidbody m_SelfRigidbody;
        private OnTriggerEnterCallbackComponent m_OnTriggerEnterCallbackComponent;

        private List<Collider> m_CollectedColliders = new();

        private static bool s_DebugForce;

        private void Awake()
        {
            m_DetectionRigidbody = m_Collider.attachedRigidbody;
            m_OnTriggerEnterCallbackComponent = m_DetectionRigidbody.gameObject.AddComponent<OnTriggerEnterCallbackComponent>();

            // Get self rigidbody
            m_SelfRigidbody = GetComponentInParent<Rigidbody>();
            if (m_SelfRigidbody == m_DetectionRigidbody)
            {
                if (m_DetectionRigidbody.transform.parent)
                    m_SelfRigidbody = m_DetectionRigidbody.transform.parent.GetComponentInParent<Rigidbody>();
                else
                    m_SelfRigidbody = null;
            }
        }
        private void OnEnable()
        {
            m_OnTriggerEnterCallbackComponent.OnTriggerEnterEvent += OnTriggerEnterEvent;
            StartCoroutine(CO_Ticking());
        }

        private void OnDisable()
        {
            m_OnTriggerEnterCallbackComponent.OnTriggerEnterEvent -= OnTriggerEnterEvent;
            StopAllCoroutines();
        }
        private void OnDestroy()
        {
            if (m_OnTriggerEnterCallbackComponent)
                Destroy(m_OnTriggerEnterCallbackComponent);
        }


        private void OnTriggerEnterEvent(Collider other)
        {
            m_CollectedColliders.Add(other);
        }


        private IEnumerator CO_Ticking()
        {
            m_DetectionRigidbody.detectCollisions = true;
            yield return new WaitForSeconds(m_TickFrequency * Random.value);

            while (true)
            {
                m_CollectedColliders.Clear();

                m_DetectionRigidbody.detectCollisions = true;
                yield return new WaitForFixedUpdate();
                m_DetectionRigidbody.detectCollisions = false;

                foreach (var other in m_CollectedColliders)
                {
                    PerformPush(other);
                }

                yield return new WaitForSeconds(m_TickFrequency - Time.fixedDeltaTime);
            }
        }

        private void PerformPush(Collider other)
        {
            if (!other)
                return;
            if (other.attachedRigidbody && m_Collider.transform.IsChildOf(other.attachedRigidbody.transform))
                return;
            if (m_PushConfig == 0)
                return;

            bool otherCanReceiveForce = other.attachedRigidbody && !other.attachedRigidbody.isKinematic;
            bool selfCanReceiveForce = m_SelfRigidbody && !m_SelfRigidbody.isKinematic;
            if (!otherCanReceiveForce && m_PushConfig == PushConfig.Other)
                return;
            if (!selfCanReceiveForce && m_PushConfig == PushConfig.Self)
                return;

            if (!Physics.ComputePenetration(other, other.transform.position, other.transform.rotation, m_Collider, m_Collider.transform.position, m_Collider.transform.rotation, out Vector3 dir, out float dist))
                return;

            Vector3 force = dir * m_PushForce;
            if (!m_Normalized)
                force *= dist;
            force = Vector3.Scale(force, m_Multiplier);

            // Push other
            if (otherCanReceiveForce)
            {
                if (m_PushConfig.HasFlag(PushConfig.Self) && selfCanReceiveForce)
                    force *= 0.5f;
                other.attachedRigidbody.AddForce(force, ForceMode.Force);
            }

            // Push self
            if (m_PushConfig.HasFlag(PushConfig.Self) && selfCanReceiveForce)
            {
                m_SelfRigidbody.AddForce(-force);
            }

            if (s_DebugForce)
                Debug.DrawLine(other.attachedRigidbody.position, other.attachedRigidbody.position + force);
        }


        public enum PushConfig
        {
            Self = 1 << 0,
            Other = 1 << 1,
        }


        [Command]
        private static void DebugPhysicsPusher()
        {
            s_DebugForce = !s_DebugForce;
        }
    }
}
