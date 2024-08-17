using Manatea;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

namespace Manatea.RootlingForest
{
    public class PullOut : MonoBehaviour
    {
        [SerializeField]
        private Rigidbody m_PullRigid;
        [SerializeField]
        private ConfigurableJoint[] m_Levers;
        [SerializeField]
        private float m_NeededForce = 75;
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
            bool pullIsHappening = false;
            for (int i = 0; i < m_Levers.Length; i++)
            {
                var lever = m_Levers[i];

                float force = lever.currentForce.magnitude;
                Debug.DrawLine(transform.position, transform.position + lever.currentForce / 50f, Color.red, Time.fixedDeltaTime, false);

                Vector3 pullDir = lever.connectedBody.transform.TransformDirection(m_PullAxis);
                Debug.DrawLine(lever.connectedBody.transform.position, lever.connectedBody.transform.position + pullDir * 2f, Color.green, Time.fixedDeltaTime, false);

                if (force > m_NeededForce && Vector3.Dot(pullDir.normalized, lever.currentForce.normalized) > 0.3f)
                {
                    m_Progress -= Time.fixedDeltaTime * m_PullSpeed;
                    pullIsHappening = true;
                }
                m_Progress = MMath.Clamp01(m_Progress);
            }

            if (!pullIsHappening)
            {
                m_Progress += Time.fixedDeltaTime * m_PullSpeed * 0.25f;
            }

            if (m_Progress <= 0)
            {
                TriggerPullOut();
            }
        }

        private void TriggerPullOut()
        {
            m_PullRigid.isKinematic = false;
            m_PullRigid.detectCollisions = false;

            Vector3 leverForce = Vector3.zero;
            for (int i = 0; i < m_Levers.Length; i++)
            {
                leverForce += m_Levers[i].currentForce;
            }
            leverForce.Normalize();

            m_PullRigid.AddForceAtPosition(Vector3.up * m_PullOutForce, transform.position - leverForce, ForceMode.VelocityChange);
            enabled = false;

            m_PulledOut.Invoke();
            StartCoroutine(DelayedDestroy(0.3f));
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
}
