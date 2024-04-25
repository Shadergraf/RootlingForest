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
    [SerializeField]
    private bool m_UseMass = false;
    [SerializeField]
    private bool m_ProjectForceVector = false;
    [SerializeField]
    private bool m_RedirectVelocity = false;

    private HashSet<Rigidbody> m_BlockerRigids = new HashSet<Rigidbody>();


    private void OnCollisionEnter(Collision collision)
    {
        if (collision.body is Rigidbody rb && !m_BlockerRigids.Contains(collision.body))
        {
            Debug.Log("Contact bounce");
            StartCoroutine(TimedBlock(collision.body as Rigidbody, 0.1f));

            Vector3 bounceDir = Vector3.up;
            Vector3 preciseBounce = (collision.contacts[0].point - transform.position).normalized;
            Vector3 horizontalBounce = Vector3.ProjectOnPlane(preciseBounce, Vector3.up).normalized;

            float dirAngle = MMath.RemapClamped(0.1f, 0.2f, 0, 1, Vector3.Dot(Vector3.up, collision.contacts[0].point - transform.position));
            bounceDir = Vector3.Lerp(horizontalBounce, bounceDir, dirAngle);

            float force = m_BounceForce;
            float velChange = 0;
            if (m_RedirectVelocity && Vector3.Dot(rb.velocity, bounceDir) < 0)
            {
                velChange += Vector3.Project(rb.velocity, bounceDir).magnitude;
            }

            if (m_ProjectForceVector)
            {
                // TODO set velocity to only project on plane if we are not moving in the bounce dir
                // if (dot(velocity, bounceDir) < 0)
                rb.velocity = Vector3.ProjectOnPlane(rb.velocity, bounceDir);
            }

            rb.velocity = bounceDir * force;
            //rb.AddForceAtPosition(bounceDir * velChange, collision.contacts[0].point, ForceMode.VelocityChange);


            if (collision.collider.TryGetComponent(out PullAbility pullAbility) && pullAbility.Target)
            {
                pullAbility.Target.velocity = bounceDir * force * 1.0f;
                //pullAbility.Target.AddForce(bounceDir * velChange, ForceMode.VelocityChange);
            }
        }
    }

    private IEnumerator TimedBlock(Rigidbody rb, float blockTime)
    {
        m_BlockerRigids.Add(rb);
        yield return new WaitForSeconds(blockTime);
        m_BlockerRigids.Remove(rb);
    }
}
