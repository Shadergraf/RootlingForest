using System.Collections;
using System.Collections.Generic;
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

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.body is Rigidbody rb)
        {
            Vector3 bounceDir = Vector3.up;

            if (m_ProjectForceVector)
            {
                rb.velocity = Vector3.ProjectOnPlane(rb.velocity, bounceDir);
                rb.AddForce(-rb.GetAccumulatedForce(Time.fixedDeltaTime), ForceMode.Force);
            }

            rb.AddForce(bounceDir * m_BounceForce, m_UseMass ? ForceMode.Impulse : ForceMode.VelocityChange);
        }
    }
}
