using Manatea;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

public class PullOut : MonoBehaviour
{
    [SerializeField]
    private Rigidbody m_PullRigid;
    [SerializeField]
    private ConfigurableJoint m_Lever;
    [SerializeField]
    private float m_PullOutDistance = 1;
    [SerializeField]
    private float m_PullSpeed = 0.4f;
    [SerializeField]
    private float m_PullOutForce = 5;
    [SerializeField]
    private Vector3 m_PullAxis = Vector3.up;
    [SerializeField]
    private UnityEvent m_PulledOut;

    private float m_Progress = 1;


    private void Start()
    {
        m_PullRigid.isKinematic = true;
        m_PullRigid.detectCollisions = false;
    }

    private void FixedUpdate()
    {
        float pulloutDist = Vector3.Distance(m_Lever.transform.TransformPoint(m_Lever.anchor), m_Lever.connectedBody.transform.TransformPoint(m_Lever.connectedAnchor));
        float force = m_Lever.currentForce.magnitude;
        Debug.DrawLine(transform.position, transform.position + m_Lever.currentForce / 50f, Color.red, Time.fixedDeltaTime, false);

        Vector3 pullDir = m_Lever.connectedBody.transform.TransformDirection(m_PullAxis);
        Debug.DrawLine(m_Lever.connectedBody.transform.position, m_Lever.connectedBody.transform.position + pullDir * 2f, Color.green, Time.fixedDeltaTime, false);

        if (force > 75 && Vector3.Dot(pullDir.normalized, m_Lever.currentForce.normalized) > 0.3f)
        {
            m_Progress -= Time.fixedDeltaTime * m_PullSpeed;
        }
        else
        {
            m_Progress += Time.fixedDeltaTime * m_PullSpeed * 0.25f;
        }
        m_Progress = MMath.Clamp01(m_Progress);

        if (m_Progress <= 0)
        {
            m_PullRigid.isKinematic = false;
            m_PullRigid.detectCollisions = false;

            m_PullRigid.AddForceAtPosition(Vector3.up * m_PullOutForce, transform.position, ForceMode.VelocityChange);
            m_Lever.connectedBody.AddForceAtPosition(Vector3.up * m_PullOutForce, transform.position, ForceMode.VelocityChange);
            enabled = false;

            m_PulledOut.Invoke();
            StartCoroutine(DelayedDestroy(0.3f));
        }
    }

    private IEnumerator DelayedDestroy(float delay)
    {
        yield return new WaitForSeconds(delay);

        m_PullRigid.detectCollisions = true;
        Destroy(this);
    }



    private void OnGUI()
    {
        if (m_Progress < 1)
        {
            MGUI.DrawWorldProgressBar(transform.position + Vector3.up, new Rect(50, 50, 100, 10), m_Progress);
        }
    }
}
