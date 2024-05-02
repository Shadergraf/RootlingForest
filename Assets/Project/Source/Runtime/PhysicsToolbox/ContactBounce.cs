using Manatea;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DefaultExecutionOrder(100)]
public class ContactBounce : MonoBehaviour
{
    [SerializeField]
    private float m_BounceForce = 1f;

    private HashSet<Rigidbody> m_BlockerRigids = new HashSet<Rigidbody>();


    private void OnCollisionEnter(Collision collision)
    {
        if (collision.body is Rigidbody rb && !m_BlockerRigids.Contains(collision.body))
        {
            //Debug.Log("Contact bounce with: " + collision.body.gameObject.name);
            StartCoroutine(TimedBlock(collision.body as Rigidbody, 0.1f));

            Vector3 targetVelocity = Vector3.up * m_BounceForce;
            StartCoroutine(CO_ApplyVelocityRepeated(rb, targetVelocity, 1));

            if (rb.TryGetComponent(out PullAbility pullAbility) && pullAbility.Target)
            {
                StartCoroutine(CO_ApplyVelocityRepeated(pullAbility.Target, targetVelocity, 1));
            }
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
            rb.velocity = velocity;
            amountOfTimes--;
            yield return new WaitForFixedUpdate();
        }
    }
}
