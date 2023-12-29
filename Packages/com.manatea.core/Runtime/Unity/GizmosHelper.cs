using UnityEditor;
using UnityEngine;

namespace Manatea
{
    public static class GizmosHelper
    {
        public static void DrawWireCircle(Vector3 position, float radius, int iterations = 32)
        {
            DrawWireCircle(position, radius, Vector3.forward, iterations);
        }
        public static void DrawWireCircle(Vector3 position, float radius, Vector3 normal, int iterations = 32)
        {
            Vector3 tangent = Vector3.Cross(normal, Vector3.up).normalized;
            if (tangent == Vector3.zero)
                tangent = Vector3.Cross(normal, Vector3.right).normalized;
            if (tangent == Vector3.zero)
                tangent = Vector3.Cross(normal, Vector3.forward).normalized;

            Vector3 lastPos = position + tangent * radius;
            for (int i = 1; i <= iterations; i++)
            {
                Vector3 newPos = position + Quaternion.AngleAxis(i / (float)iterations * 360, normal) * tangent * radius;
                Gizmos.DrawLine(lastPos, newPos);
                lastPos = newPos;
            }
        }
    }
}