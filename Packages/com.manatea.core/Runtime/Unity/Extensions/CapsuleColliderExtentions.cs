using Manatea;

namespace UnityEngine
{
    public static class CapsuleColliderExtensions
    {
        public static void GetGlobalParams(this CapsuleCollider capsuleCollider, out Vector3 p1, out Vector3 p2, out float radius)
        {
            Transform transform = capsuleCollider.transform;

            Vector3 capsuleDir = capsuleCollider.direction == 0 ? Vector3.right : (capsuleCollider.direction == 1 ? Vector3.up : Vector3.forward);
            radius = capsuleCollider.radius * MMath.Max(Vector3.ProjectOnPlane(transform.localScale, capsuleDir));
            float scaledHeight = MMath.Max(capsuleCollider.height, capsuleCollider.radius * 2) * Vector3.Dot(transform.localScale, capsuleDir);

            float capsuleHalfHeightWithoutHemisphereScaled = scaledHeight / 2 - radius;
            Vector3 pCenter = transform.TransformPoint(capsuleCollider.center);
            Vector3 pDir = transform.TransformDirection(Vector2.up);
            p1 = pCenter + pDir * capsuleHalfHeightWithoutHemisphereScaled;
            p2 = pCenter - pDir * capsuleHalfHeightWithoutHemisphereScaled;
        }
    }
}

