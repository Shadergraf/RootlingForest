using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Manatea;
using UnityEngine.Events;

namespace Manatea.RootlingForest
{
    public class Popper : MonoBehaviour
    {
        public int m_PopCount = 15;
        public float m_PopDelay = 0.2f;
        public float m_PopForce = 2;
        public float m_PopRotation = 2;
        public UnityEvent m_PopResponse;
        public UnityEvent m_PoppingFinished;

        public GameObject[] m_Poppers;

        public bool CurrentlyPopping
        { get; private set; }

        public void Pop()
        {
            if (CurrentlyPopping)
            {
                return;
            }

            CurrentlyPopping = true;
            StartCoroutine(CO_PoppingSequence());
        }

        private IEnumerator CO_PoppingSequence()
        {
            var shuffeledIndices = ShuffledIndices(m_Poppers.Length);

            Rigidbody rigid = GetComponent<Rigidbody>();
            for (int i = 0; i < m_PopCount; i++)
            {
                m_PopResponse.Invoke();

                rigid.AddForce(Random.insideUnitSphere * m_PopForce, ForceMode.Impulse);
                Vector3 randomCross = Random.onUnitSphere;
                Vector3 randomRotationAngle = Vector3.Cross(Random.onUnitSphere, randomCross) * (Random.value * 2 - 1);
                rigid.AddForceAtPosition(randomRotationAngle * m_PopRotation, rigid.worldCenterOfMass + randomCross, ForceMode.Impulse);
                rigid.AddForceAtPosition(randomRotationAngle * -m_PopRotation, rigid.worldCenterOfMass - randomCross, ForceMode.Impulse);

                if (m_Poppers.Length > 0)
                {
                    int index = (int)(i / (float)m_PopCount * m_Poppers.Length);
                    m_Poppers[shuffeledIndices[index]].SetActive(false);
                }

                if (i < m_PopCount)
                {
                    yield return new WaitForSeconds(MMath.Lerp(0.01f, m_PopDelay, MMath.Sqrt(Random.value)));
                }
            }

            m_PoppingFinished.Invoke();
        }

        private static int[] ShuffledIndices(int n)
        {
            var result = new int[n];
            for (var i = 0; i < n; i++)
            {
                var j = Random.Range(0, i + 1);
                if (i != j)
                {
                    result[i] = result[j];
                }
                result[j] = i;
            }
            return result;
        }
    }
}
