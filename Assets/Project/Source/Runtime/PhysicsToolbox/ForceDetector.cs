using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Manatea.GameplaySystem;
using UnityEngine.InputSystem.Utilities;

namespace Manatea.RootlingForest
{
    public class ForceDetector : BaseForceDetector
    {
        [SerializeField]
        private GameObject[] m_SpawnObjects;
        [SerializeField]
        private bool m_DestroyObject;
        [SerializeField]
        private float m_Force;
        [SerializeField, Range(0, 1)]
        private float m_RadialForce = 1;
        [SerializeField]
        private bool m_EnableDebugGraphs;
        [SerializeField]
        private bool m_OnlyBreakWhenBeingHit;
        // Events
        [SerializeField]
        private UnityEvent m_ForceDetected;

        protected override void ForceDetected(Vector3 force)
        {
            m_ForceDetected.Invoke();

            if (Config.HealthAttribute && AttributeOwner)
            {
                AttributeOwner.ChangeAttributeBaseValue(Config.HealthAttribute, v => v - 1);
            }

            for (int i = 0; i < m_SpawnObjects.Length; ++i)
            {
                m_SpawnObjects[i].SetActive(true);
                m_SpawnObjects[i].transform.SetParent(null);
                Rigidbody rigid = m_SpawnObjects[i].GetComponent<Rigidbody>();
                rigid.linearVelocity = Rigidbody.linearVelocity;
                rigid.angularVelocity = Rigidbody.angularVelocity;

                Vector3 randomDir = Random.onUnitSphere;
                Vector3 radialDir = rigid.position - Rigidbody.position;
                if (radialDir.magnitude < 0.001f)
                {
                    radialDir = randomDir;
                }
                Vector3 forceDir = Vector3.Lerp(randomDir, radialDir, m_RadialForce).normalized;
                //rigid.AddForce(force * m_Force, m_UseMass ? ForceMode.Impulse : ForceMode.VelocityChange);
                rigid.linearVelocity += forceDir * (m_Force);
            }

            if (m_DestroyObject)
            {
                Destroy(Rigidbody.gameObject);
            }
        }

        public void Break()
        {
            ForceDetected(transform.forward);
        }
    }
}
