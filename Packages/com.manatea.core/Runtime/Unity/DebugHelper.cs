using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Manatea
{
    public static class DebugHelper
    {
        public static void DrawWireCircle(Vector3 position, float radius, int iterations = 32)
            => DrawWireCircle(position, radius, Vector3.forward, Color.white, 0, true, iterations);
        public static void DrawWireCircle(Vector3 position, float radius, Vector3 normal, Color color, float duration = 0.0f, bool depthTest = true, int iterations = 32)
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
                Debug.DrawLine(lastPos, newPos, color, duration, depthTest);
                lastPos = newPos;
            }
        }

        public static void DrawWireSphere(Vector3 position, float radius, Color color, float duration = 0.0f, bool depthTest = true, int iterations = 32)
        {
            DrawWireCircle(position, radius, Vector3.right, color, duration, depthTest, iterations);
            DrawWireCircle(position, radius, Vector3.up, color, duration, depthTest, iterations);
            DrawWireCircle(position, radius, Vector3.forward, color, duration, depthTest, iterations);
        }


        public static void DrawQuaternion(Vector3 position, Quaternion quaternion, float scale = 1, float duration = 0.0f, bool depthTest = true)
        {
            Debug.DrawLine(position, position + quaternion * Vector3.right   * scale, Color.red,   duration, depthTest);
            Debug.DrawLine(position, position + quaternion * Vector3.up      * scale, Color.green, duration, depthTest);
            Debug.DrawLine(position, position + quaternion * Vector3.forward * scale, Color.blue,  duration, depthTest);
        }
    }
}