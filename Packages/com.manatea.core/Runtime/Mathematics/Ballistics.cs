using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Manatea
{
    public static class Ballistics
    {
        private static Vector3 TransformPointFromTrajectorySpace(Vector2 point, Vector3 trajectoryStart, Vector3 upDir, Vector3 lateralDir)
        {
            Vector2 pts = point;

            Vector3 pws = Vector3.zero;
            pws += lateralDir * pts.x;
            pws += upDir.normalized * pts.y;
            return pws + trajectoryStart;
        }
        private static Vector3 TransformDirFromTrajectorySpace(Vector2 point, Vector3 upDir, Vector3 lateralDir)
            => TransformPointFromTrajectorySpace(point, Vector3.zero, upDir, lateralDir);
        private static Vector2 TransformPointToTrajectorySpace(Vector3 point, Vector3 trajectoryStart, Vector3 upDir)
        {
            Vector3 p = point - trajectoryStart;

            Vector2 pts = Vector2.zero;
            pts.x = Vector3.ProjectOnPlane(p, upDir).magnitude;
            pts.y = Vector3.Dot(p, upDir);
            return pts;
        }
        private static Vector2 TransformDirToTrajectorySpace(Vector3 point, Vector3 upDir)
            => TransformPointToTrajectorySpace(point, Vector3.zero, upDir);

        // Gravity has to be positive(!)
        private static bool CalculateInitialVelocity(Vector2 targetPos, float speed, float gravity, out Vector2 velA, out Vector2 velB)
        {
            float v = speed;
            float vSqua = v * v;
            float vQuar = vSqua * vSqua;

            float det = vQuar - (gravity * (gravity * targetPos.x * targetPos.x + 2 * targetPos.y * vSqua));

            if (det < 0)
            {
                velA = Vector2.zero;
                velB = Vector2.zero;
                return false;
            }

            float detSqrt = MMath.Sqrt(det);
            float t1Launch = MMath.Atan((vSqua + detSqrt) / (gravity * targetPos.x));
            float t2Launch = MMath.Atan((vSqua - detSqrt) / (gravity * targetPos.x));

            velA = new Vector2(MMath.Cos(t1Launch), MMath.Sin(t1Launch)) * speed;
            velB = new Vector2(MMath.Cos(t2Launch), MMath.Sin(t2Launch)) * speed;

            return true;
        }
        public static bool CalculateInitialVelocity(Vector3 startPos, Vector3 targetPos, float speed, Vector3 gravity, out Vector3 velA, out Vector3 velB)
        {
            Vector3 up = -gravity.normalized;
            Vector3 lateral = Vector3.ProjectOnPlane(targetPos - startPos, up).normalized;

            Vector2 tsTargetPos = TransformPointToTrajectorySpace(targetPos, startPos, up);
            bool valid = CalculateInitialVelocity(tsTargetPos, speed, gravity.magnitude, out Vector2 tsVelA, out Vector2 tsVelB);
            if (!valid)
            {
                velA = Vector2.zero;
                velB = Vector2.zero;
                return false;
            }

            velA = TransformDirFromTrajectorySpace(tsVelA, up, lateral);
            velB = TransformDirFromTrajectorySpace(tsVelB, up, lateral);
            return true;
        }
    }
}