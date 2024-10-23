using Manatea;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Manatea.RootlingForest
{
    [DefaultExecutionOrder(100)]
    public class ContactBounce : MonoBehaviour
    {
        [SerializeField]
        private Vector3 m_Direction = Vector3.forward;
        [SerializeField]
        private bool m_NormalizeDirection = true;
        [SerializeField]
        private bool m_UseGlobalSpace = false;
        [SerializeField]
        private float m_Force = 1f;
        [SerializeField]
        private float m_PreservePerpendicularComponent = 0;
        [SerializeField]
        private float m_OppositeForce = 1;
        [SerializeField]
        private int m_Iterations = 1;
        [SerializeField]
        private List<Collider> m_IncludeColliders;

        private HashSet<GameObject> m_BlockerRigids = new HashSet<GameObject>();


        private void OnCollisionEnter(Collision collision)
        {
            if (collision.body && m_BlockerRigids.Contains(collision.gameObject))
            {
                return;
            }
            if (m_IncludeColliders.Count > 0)
            {
                List<ContactPoint> pairs = new List<ContactPoint>();
                collision.GetContacts(pairs);

                bool validHit = false;
                for (int i = 0; i < pairs.Count; i++)
                {
                    if (m_IncludeColliders.Contains(pairs[i].thisCollider))
                    {
                        validHit = true;
                        break;
                    }
                }

                if (!validHit)
                {
                    return;
                }
            }


            Vector3 targetVelocity = m_Direction;
            if (!m_UseGlobalSpace)
            {
                targetVelocity = transform.TransformDirection(targetVelocity);
            }
            if (m_NormalizeDirection)
            {
                targetVelocity.Normalize();
            }
            targetVelocity *= m_Force;
            Debug.DrawLine(transform.position, transform.position + targetVelocity.normalized, Color.blue, 0.5f);

            if (m_OppositeForce != 0 && TryGetComponent(out Rigidbody selfRigid) && !m_BlockerRigids.Contains(gameObject))
            {
                StartCoroutine(CO_ApplyVelocityRepeated(selfRigid, targetVelocity * -1 * m_OppositeForce, m_Iterations));
                StartCoroutine(CO_TimedBlock(collision.body as Rigidbody, 0.1f));
            }


            Rigidbody rb = collision.body as Rigidbody;
            if (rb)
            {
                StartCoroutine(CO_ApplyVelocityRepeated(rb, targetVelocity, m_Iterations));
                StartCoroutine(CO_TimedBlock(rb, 0.1f));

                if (rb.TryGetComponent(out GrabAbility pullAbility) && pullAbility.Target)
                {
                    StartCoroutine(CO_ApplyVelocityRepeated(pullAbility.Target, targetVelocity, m_Iterations));
                    StartCoroutine(CO_TimedBlock(pullAbility.Target, 0.1f));
                }
            }
        }

        private IEnumerator CO_TimedBlock(Rigidbody rb, float blockTime)
        {
            if (rb)
            {
                m_BlockerRigids.Add(rb.gameObject);
            }

            yield return new WaitForSeconds(blockTime);

            if (rb)
            {
                m_BlockerRigids.Remove(rb.gameObject);
            }
        }

        private IEnumerator CO_ApplyVelocityRepeated(Rigidbody rb, Vector3 velocity, int amountOfTimes)
        {
            float consecutivePerpendicularComponentMult = MMath.Pow(m_PreservePerpendicularComponent, 1f / amountOfTimes);

            rb.linearVelocity = Vector3.ProjectOnPlane(rb.linearVelocity, velocity.normalized) * m_PreservePerpendicularComponent;

            for (int i = 0; i < amountOfTimes; i++)
            {
                if (!rb)
                {
                    yield break;
                }

                if (!rb.isKinematic)
                {
                    rb.linearVelocity += velocity / amountOfTimes;
                }

                yield return new WaitForFixedUpdate();
            }
        }
    }
}
