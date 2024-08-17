using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Manatea;
using UnityEngine.Events;

namespace Manatea.RootlingForest
{
    public class CollisionDetector : MonoBehaviour
    {
        [SerializeField]
        private float m_ImpulseMagnitude = 5;
        [SerializeField]
        private GameObject[] m_SpawnObjects;
        [SerializeField]
        private bool m_DestroyObject;
        [SerializeField]
        private float m_Force;
        [SerializeField, Range(0, 1)]
        private float m_RadialForce = 1;
        [SerializeField]
        private float m_ImpulseForceContribution = 0;
        [SerializeField]
        private bool m_UseMass;

        [SerializeField]
        private UnityEvent m_ForceDetected;


        private void OnCollisionEnter(Collision collision)
        {
            if (!enabled)
            {
                return;
            }

            if (collision.impulse.magnitude > m_ImpulseMagnitude)
            {
                ForceDetected(collision.impulse);
            }
        }
        private void OnCollisionStay(Collision collision)
        {
            if (!enabled)
            {
                return;
            }

            if (collision.impulse.magnitude > m_ImpulseMagnitude)
            {
                ForceDetected(collision.impulse);
            }
        }

        public void ForceDetected(Vector3 impulse)
        {
            m_ForceDetected.Invoke();

            Rigidbody sourceRigid = GetComponent<Rigidbody>();
            for (int i = 0; i < m_SpawnObjects.Length; ++i)
            {
                m_SpawnObjects[i].SetActive(true);
                m_SpawnObjects[i].transform.SetParent(transform.parent);
                Rigidbody rigid = m_SpawnObjects[i].GetComponent<Rigidbody>();
                rigid.velocity = sourceRigid.velocity;
                rigid.angularVelocity = sourceRigid.angularVelocity;

                Vector3 randomDir = Random.onUnitSphere;
                Vector3 radialDir = rigid.position - sourceRigid.position;
                if (radialDir.magnitude < 0.001f)
                {
                    radialDir = randomDir;
                }
                Vector3 forceDir = Vector3.Lerp(randomDir, radialDir, m_RadialForce).normalized;
                //rigid.AddForce(force * m_Force, m_UseMass ? ForceMode.Impulse : ForceMode.VelocityChange);
                rigid.velocity += forceDir * m_Force + impulse * m_ImpulseForceContribution;
            }

            if (m_DestroyObject)
            {
                Destroy(gameObject);
            }
        }
    }
}
