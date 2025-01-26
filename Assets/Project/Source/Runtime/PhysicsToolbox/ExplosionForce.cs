using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Manatea.RootlingForest
{
    public class ExplosionForce : MonoBehaviour
    {
        [SerializeField]
        private Transform m_CenterTransform;
        [SerializeField]
        private Vector3 m_CenterPosition;
        [SerializeField]
        private float m_Radius = 1;
        [SerializeField]
        private float m_Strength = 1;
        [SerializeField]
        [Min(0)]
        private float m_Duration = 0;
        [SerializeField]
        private bool m_IgnoreMass;
        [SerializeField]
        private Rigidbody[] m_OnlyApplyToRigidbodies;

        private List<float> m_DetonationTimes = new(1);

        private static Collider[] m_Overlaps = new Collider[16];
        private static int m_OverlapCount;


        private void OnEnable()
        {
            if (m_DetonationTimes.Count == 0)
            {
                m_DetonationTimes.Add(m_Duration);
            }
        }

        private void FixedUpdate()
        {
            Vector3 explosionPosition = GetExplosionCenter();

            // Collect rigidbodies
            HashSet<Rigidbody> rigidbodies = new HashSet<Rigidbody>();
            if (m_OnlyApplyToRigidbodies.Length == 0)
            {
                int layerMask = LayerMaskExtensions.CalculatePhysicsLayerMask(gameObject.layer);
                while (true)
                {
                    m_OverlapCount = Physics.OverlapSphereNonAlloc(explosionPosition, m_Radius, m_Overlaps, layerMask, QueryTriggerInteraction.Ignore);
                    if (m_OverlapCount == m_Overlaps.Length)
                    {
                        System.Array.Resize(ref m_Overlaps, m_Overlaps.Length * 2);
                    }
                    else
                    {
                        break;
                    }
                }

                for (int i = 0; i < m_OverlapCount; i++)
                {
                    if (m_Overlaps[i].attachedRigidbody)
                    {
                        rigidbodies.Add(m_Overlaps[i].attachedRigidbody);
                    }
                }
            }
            else
            {
                for (int i = 0; i < m_OnlyApplyToRigidbodies.Length; i++)
                {
                    if (m_OnlyApplyToRigidbodies[i])
                    {
                        rigidbodies.Add(m_OnlyApplyToRigidbodies[i]);
                    }
                }
            }

            // Apply forces
            for (int i = 0; i < m_DetonationTimes.Count; i++)
            {
                foreach (var rigid in rigidbodies)
                {
                    Vector3 forceVector = rigid.position - explosionPosition;

                    // TODO bad, should be adjusted by exposed parameters instead of here
                    forceVector = (forceVector * 5).ClampMagnitude(0, 1);

                    rigid.AddForceAtPosition(forceVector * m_Strength, explosionPosition, m_IgnoreMass ? ForceMode.VelocityChange : ForceMode.Impulse);
                }

                // Time
                m_DetonationTimes[i] -= Time.fixedDeltaTime;
                if (m_DetonationTimes[i] <= 0)
                {
                    m_DetonationTimes.RemoveAt(i);
                    i--;
                }
            }

            if (m_DetonationTimes.Count == 0)
            {
                enabled = false;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 center = GetExplosionCenter();
            Gizmos.DrawWireSphere(center, m_Radius);
        }


        public Vector3 GetExplosionCenter()
        {
            Vector3 explosionPosition = m_CenterPosition;
            if (m_CenterTransform)
                explosionPosition = m_CenterTransform.TransformPoint(explosionPosition);
            else
                explosionPosition = transform.TransformPoint(explosionPosition);
            return explosionPosition;
        }

        public void Detonate()
        {
            enabled = true;

            m_DetonationTimes.Add(m_Duration);
        }
    }
}
