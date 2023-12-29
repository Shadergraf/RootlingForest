using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Manatea
{
    public static class MRandom
    {
        public static bool InsideTwoCircleIntersection(Vector2 p1, float r1, Vector2 p2, float r2, out Vector2 result)
        {
            float dist = (p1 - p2).magnitude;
            if (dist >= r1 + r2)
            {
                result = Vector2.zero;
                return false;
            }

            Vector2 bigPos, smallPos;
            float bigRad, smallRad;
            if (r1 > r2)
            {
                bigPos = p1;
                smallPos = p2;
                bigRad = r1;
                smallRad = r2;
            }
            else
            {
                bigPos = p2;
                smallPos = p1;
                bigRad = r2;
                smallRad = r1;
            }

            // Project values
            Vector2 offset = bigPos;
            bigPos = Vector2.zero;
            smallPos -= offset;
            Vector2 projSmallPos = bigPos + Vector2.right * (smallPos - bigPos).magnitude;

            float right = MMath.Min(bigPos.x + bigRad, projSmallPos.x + smallRad);
            float left = MMath.Max(bigPos.x - bigRad, projSmallPos.x - smallRad);
            float top = MMath.Min(bigPos.y + bigRad, projSmallPos.y + smallRad);
            float bottom = MMath.Max(bigPos.y - bigRad, projSmallPos.y - smallRad);

            if (projSmallPos.x + smallRad > bigRad)
            {
                float a = (bigRad * bigRad - smallRad * smallRad + dist * dist) / (2 * dist);
                float h = MMath.Sqrt(bigRad * bigRad - a * a);
                float cx2 = bigPos.x + a * (projSmallPos.x - bigPos.x) / dist;
                float xIntersect = cx2 + h * (projSmallPos.y - bigPos.y) / dist;
                if (projSmallPos.x > xIntersect)
                {
                    top = MMath.Min(top, h);
                    bottom = MMath.Max(bottom, -h);
                }
            }

            float bigRadSqrt = bigRad * bigRad;
            float smallRadSqrt = smallRad * smallRad;
            int counter = 10;
            do
            {
                counter--;
                result = new Vector2(Random.Range(left, right), Random.Range(bottom, top));
            }
            while (counter > 0 && ((bigPos - result).sqrMagnitude > bigRadSqrt || (projSmallPos - result).sqrMagnitude > smallRadSqrt));

            if (counter == 0)
                return false;

            float angle = MMath.SignedAngle(smallPos - bigPos, Vector2.right);
            result = Quaternion.AngleAxis(angle * MMath.Rad2Deg, -Vector3.forward) * result;
            result += offset;

            return true;
        }
    }
}
