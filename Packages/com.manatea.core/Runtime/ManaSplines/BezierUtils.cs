using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Manatea.SplineTool
{
    public static class BezierUtils
    {

        /// <summary>
        /// Evaluate the Quadratic beziercurve at time t
        /// </summary>
        public static Vector3 EvaluateBezier(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
        {
            Vector3 iA = Vector3.Lerp(a, b, t);
            Vector3 iB = Vector3.Lerp(b, c, t);
            Vector3 iC = Vector3.Lerp(c, d, t);

            Vector3 iiA = Vector3.Lerp(iA, iB, t);
            Vector3 iiB = Vector3.Lerp(iB, iC, t);

            return Vector3.Lerp(iiA, iiB, t);
        }

        /// <summary>
        /// Calculate the points along this curve
        /// <param name="step">the step length in relative time [0,1] </param>
        /// </summary>
        public static Vector3[] GetCurvePoints(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float step)
        {
            step = Mathf.Clamp01(step);

            Vector3[] subPoints = new Vector3[Mathf.RoundToInt(1 / step) + 1];

            for (int i = 0; i < subPoints.Length; i ++)
            {
                subPoints[i] = EvaluateBezier(a,b,c,d, i * step);
            }

            return subPoints;
        }

        /// <summary>
        /// Calculate the points along this curve and the distances between the points
        /// <param name="step">the step length in relative time [0,1] </param>
        /// </summary>
        public static (Vector3 position, float arcLength)[] GetCurvePointsAndArcLengths(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float step)
        {
            var subPoints = GetCurvePoints(a, b, c, d, step);

            var pointMeasures = new (Vector3 position, float distanceToNext)[subPoints.Length];

            float currentLength = 0;

            pointMeasures[0] = (a, 0);

            for (int i = 1; i < subPoints.Length; i++)
            {
                currentLength += Vector3.Distance(subPoints[i - 1],  subPoints[i]);
                pointMeasures[i] = (subPoints[i], currentLength);
            }

            return pointMeasures;
        }


        public static void CalculateArcLengths(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float step, out float[] arcLengths, out Vector3[] arcPoints)
        {
            step = Mathf.Clamp(step, 0.01F, 1);

            arcLengths  = new float [Mathf.RoundToInt(1 / step) + 1]; 
            arcPoints   = new Vector3 [Mathf.RoundToInt(1 / step) + 1]; 

            Vector3 pointA = a;
            Vector3 pointB;

            for (int i = 0; i < arcLengths.Length; i++)
            {
                var t = Mathf.Clamp01(i * step);

                if(i == arcLengths.Length - 1)
                    t = 1;

                pointB = EvaluateBezier(a, b, c, d, t);


                arcLengths[i]   = i > 0 ? arcLengths[i - 1] + Vector3.Distance(pointA, pointB) : Vector3.Distance(pointA, pointB);
                arcPoints[i]    = pointB;

                pointA = pointB;
            }
        }


        /// <summary>
        /// Calculate the points along this curve such that they are spaced out evenly
        /// <param name="step">the step length in relative time [0,1] </param>
        /// </summary>
        public static Vector3[] GetEquidistantCurvePoints(Vector3 a, Vector3 b, Vector3 c, Vector3 d, int subpoints)
        {
            var points = new Vector3[subpoints];

            var straightDistance = Vector3.Distance(a,b) + Vector3.Distance(b,c) + Vector3.Distance(c,d);

            var precision = 1 / straightDistance; //TODO this is somewhat arbitrary

            var measurements = GetCurvePointsAndArcLengths(a, b, c, d, precision);

            float step = measurements[measurements.Length - 1].arcLength / (subpoints - 1);

            for (int i = 0; i < points.Length; i++)
            {
                var targetDistance = step * i;

                for (int x = 0; x < measurements.Length - 1; x++)
                {
                    if(measurements[x + 1].arcLength >= targetDistance)
                    {
                        var segmentLength = Mathf.Abs(measurements[x + 1].arcLength - measurements[x].arcLength);

                        points[i] = Vector3.Lerp(measurements[x].position, measurements[x + 1].position, (targetDistance - measurements[x].arcLength) / segmentLength);

                        break;
                    }
                }

            }

            points[0] = a;
            points[points.Length - 1] = d;

            return points;
        }

    }

}
