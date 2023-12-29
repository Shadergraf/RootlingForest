using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;

namespace Manatea
{
    public class RandomTester : MonoBehaviour
    {
        public Transform m_Circle1;
        public Transform m_Circle2;
        public float iterations = 1000;
        public float testIterations = 10000;
        public float size = 0.1f;



        private void OnDrawGizmos()
        {
            GizmosHelper.DrawWireCircle(m_Circle1.position, m_Circle1.lossyScale.x, 128);
            GizmosHelper.DrawWireCircle(m_Circle2.position, m_Circle2.lossyScale.x, 128);

            for (int i = 0; i < iterations; i++)
            {
                if (MRandom.InsideTwoCircleIntersection(m_Circle1.position, m_Circle1.lossyScale.x, m_Circle2.position, m_Circle2.lossyScale.x, out Vector2 result))
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(result, size);
                }
            }
        }
    }
}
