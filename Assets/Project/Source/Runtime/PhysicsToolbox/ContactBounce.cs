using Manatea;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
            StartCoroutine(CO_ApplyVelocityRepeated(selfRigid, targetVelocity * -1 * m_OppositeForce, 4));
            StartCoroutine(TimedBlock(collision.body as Rigidbody, 0.1f));
        }


        Rigidbody rb = collision.body as Rigidbody;
        if (rb)
        {
            StartCoroutine(TimedBlock(rb, 0.1f));

            StartCoroutine(CO_ApplyVelocityRepeated(rb, targetVelocity, 4));

            if (rb.TryGetComponent(out PullAbility pullAbility) && pullAbility.Target)
            {
                StartCoroutine(CO_ApplyVelocityRepeated(pullAbility.Target, targetVelocity, 1));
            }
        }
    }

    private IEnumerator TimedBlock(Rigidbody rb, float blockTime)
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
        while (amountOfTimes > 0 && rb != null)
        {
            if (!rb.isKinematic)
            {
                Vector3 newVelocity = velocity;
                newVelocity += Vector3.ProjectOnPlane(rb.velocity, velocity.normalized) * m_PreservePerpendicularComponent;
                rb.velocity = newVelocity;
            }
            amountOfTimes--;
            yield return new WaitForFixedUpdate();
        }
    }
}
