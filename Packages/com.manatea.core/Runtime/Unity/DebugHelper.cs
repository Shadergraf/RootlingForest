using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

namespace Manatea
{
    public static class DebugHelper
    {
        public static void DrawWireCircle(Vector3 position, float radius, Color color, int iterations = 16)
            => DrawWireCircle(position, radius, Vector3.forward, color, 0, true, iterations);
        public static void DrawWireCircle(Vector3 position, float radius, Vector3 normal, Color color, float duration = 0.0f, bool depthTest = true, int iterations = 16, Vector3 tangent = new Vector3(), float fraction = 1)
        {
            if (tangent == Vector3.zero)
                tangent = Vector3.Cross(normal, Vector3.up).normalized;
            if (tangent == Vector3.zero)
                tangent = Vector3.Cross(normal, Vector3.right).normalized;
            if (tangent == Vector3.zero)
                tangent = Vector3.Cross(normal, Vector3.forward).normalized;

            Vector3 lastPos = position + tangent * radius;
            for (int i = 1; i <= iterations; i++)
            {
                Vector3 newPos = position + Quaternion.AngleAxis(i / (float)iterations * 360 * fraction, normal) * tangent * radius;
                Debug.DrawLine(lastPos, newPos, color, duration, depthTest);
                lastPos = newPos;
            }
        }

        public static void DrawWireSphere(Vector3 position, float radius, Color color, float duration = 0.0f, bool depthTest = true, int iterations = 16)
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


        public static void DrawWireCapsule(Vector3 positionA, Vector3 positionB, float radius, Color color, float duration = 0.0f, bool depthTest = true, int iterations = 16, Vector3 normal = new Vector3())
        {
            Vector3 tangent = (positionB - positionA).normalized;
            if (normal == Vector3.zero)
                normal = Vector3.Cross(tangent, Vector3.up);
            if (normal == Vector3.zero)
                normal = Vector3.Cross(tangent, Vector3.right);
            if (normal == Vector3.zero)
                normal = Vector3.Cross(tangent, Vector3.forward);
            Vector3 binormal = Vector3.Cross(tangent, normal);

            DrawWireCircle(positionA, radius,  -normal, color, duration, depthTest, iterations / 2, binormal, 0.5f);
            DrawWireCircle(positionB, radius,   normal, color, duration, depthTest, iterations / 2, binormal, 0.5f);
            DrawWireCircle(positionA, radius,  binormal, color, duration, depthTest, iterations / 2, normal, 0.5f);
            DrawWireCircle(positionB, radius, -binormal, color, duration, depthTest, iterations / 2, normal, 0.5f);

            DrawWireCircle(positionA, radius, tangent, color, duration, depthTest, iterations, normal);
            DrawWireCircle(positionB, radius, tangent, color, duration, depthTest, iterations, normal);

            Debug.DrawLine(positionA +  normal * radius, positionB + normal * radius, color, duration, depthTest);
            Debug.DrawLine(positionA -  normal * radius, positionB - normal * radius, color, duration, depthTest);
            Debug.DrawLine(positionA + binormal * radius, positionB + binormal * radius, color, duration, depthTest);
            Debug.DrawLine(positionA - binormal * radius, positionB - binormal * radius, color, duration, depthTest);
        }

    }
}