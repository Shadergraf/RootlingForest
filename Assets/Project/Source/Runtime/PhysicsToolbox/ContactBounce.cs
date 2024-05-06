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

    private HashSet<Rigidbody> m_BlockerRigids = new HashSet<Rigidbody>();


    private void OnCollisionEnter(Collision collision)
    {
        Rigidbody rb = collision.body as Rigidbody;
        if (!rb)
        {
            return;
        }
        if (m_BlockerRigids.Contains(collision.body))
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


        StartCoroutine(TimedBlock(collision.body as Rigidbody, 0.1f));

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

        StartCoroutine(CO_ApplyVelocityRepeated(rb, targetVelocity, 1));
        if (m_OppositeForce != 0 && TryGetComponent(out Rigidbody selfRigid))
        {
            StartCoroutine(CO_ApplyVelocityRepeated(selfRigid, targetVelocity * -1 * m_OppositeForce, 1));
            StartCoroutine(TimedBlock(collision.body as Rigidbody, 0.05f));
        }

        if (rb.TryGetComponent(out PullAbility pullAbility) && pullAbility.Target)
        {
            StartCoroutine(CO_ApplyVelocityRepeated(pullAbility.Target, targetVelocity, 1));
        }
    }

    private IEnumerator TimedBlock(Rigidbody rb, float blockTime)
    {
        m_BlockerRigids.Add(rb);
        yield return new WaitForSeconds(blockTime);
        m_BlockerRigids.Remove(rb);
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
