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



        /// <summary>
        /// Evaluates a 2D parabola of form y=ax²+bx that starts in (0,0)
        /// </summary>
        /// <returns> The y position of the evaluated parabola. </returns>
        public static float Parabola(float a, float b, float x)
        {
            return a * x * x + b * x;
        }
        /// <summary>
        /// Calculates the parabolic coefficients a and b that define a parabola matching a projectile path with initial velocity and signed gravity
        /// </summary>
        /// <returns> A tuple of parabolic coefficients a and b. </returns>
        public static (float a, float b) CalculateParabola(Vector2 velocity, float gravity)
        {
            float a = gravity / (2 * velocity.x * velocity.x);
            float b = velocity.y / velocity.x;
            return (a, b);
        }
        /// <summary>
        /// Calculates the closest location on a 2D parabola defined by a and b that starts in (0,0) to a specific point
        /// </summary>
        public static Vector2 GetClosestPointOnParabola(float a, float b, Vector2 point)
        {
            // ChatGPT to the rescue

            // TODO return separate solutions for a = 0

            // Coefficients of the cubic equation in the form Ax^3 + Bx^2 + Cx + D = 0
            float A = 2 * a * a;
            float B = 3 * a * b;
            float C = -2 * a * point.y + b * b + 1;
            float D = -b * point.y - point.x;

            // Convert to a depressed cubic t^3 + pt + q = 0 using the substitution x = t - B/(3A)
            float p = (3 * A * C - B * B) / (3 * A * A);
            float q = (2 * B * B * B - 9 * A * B * C + 27 * A * A * D) / (27 * A * A * A);

            // Calculate the discriminant
            float discriminant = q * q / 4 + p * p * p / 27;

            float[] roots;
            if (discriminant > 0)
            {
                // One real root and two complex roots
                float sqrtDiscriminant = MMath.Sqrt(discriminant);
                float u = MMath.Cbrt(-q / 2 + sqrtDiscriminant);
                float v = MMath.Cbrt(-q / 2 - sqrtDiscriminant);
                float root1 = u + v - B / (3 * A);
                roots = new float[] { root1 };
            }
            else if (discriminant == 0)
            {
                // All roots are real and at least two are equal
                float u = MMath.Cbrt(-q / 2);
                float root1 = 2 * u - B / (3 * A);
                float root2 = -u - B / (3 * A);
                roots = new float[] { root1, root2 };
            }
            else
            {
                // Three distinct real roots
                float rho = MMath.Sqrt(-p * p * p / 27);
                float theta = MMath.Acos(-q / (2 * rho));
                float rhoCbrt = MMath.Cbrt(rho);
                float root1 = 2 * rhoCbrt * MMath.Cos(theta / 3) - B / (3 * A);
                float root2 = 2 * rhoCbrt * MMath.Cos((theta + 2 * MMath.PI) / 3) - B / (3 * A);
                float root3 = 2 * rhoCbrt * MMath.Cos((theta + 4 * MMath.PI) / 3) - B / (3 * A);
                roots = new float[] { root1, root2, root3 };
            }

            // Only return the closest point
            float closestX = roots[0];
            float closestY = Parabola(a, b, closestX);
            for (int i = 0; i < roots.Length; i++)
            {
                float py = Parabola(a, b, roots[i]);
                if (Vector2.Distance(new Vector2(closestX, closestY), point) > Vector2.Distance(new Vector2(roots[i], py), point))
                {
                    closestX = roots[i];
                    closestY = py;
                }
            }

            return new Vector2(closestX, closestY);
        }
    }
}