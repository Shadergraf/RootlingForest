// Reference
// https://github.com/FreyaHolmer/Mathfs

using UnityEngine;

namespace Manatea
{
    public static class MGeometry
    {
        /// <summary> Determines if the given point is inside a polygon. </summary>
        /// <param name="polygon"> The vertices of the polygon. </param>
        /// <param name="testPoint"> The point to test<. /param>
        /// <returns> Returns true if the point is inside the polygon, false otherwise. </returns>
        public static bool IsPointInPolygon(Vector2[] polygon, Vector2 testPoint)
        {
            bool result = false;
            int j = polygon.Length - 1;
            for (int i = 0; i < polygon.Length; i++)
            {
                if (polygon[i].y < testPoint.y && polygon[j].y >= testPoint.y || polygon[j].y < testPoint.y && polygon[i].y >= testPoint.y)
                {
                    if (polygon[i].x + (testPoint.y - polygon[i].y) / (polygon[j].y - polygon[i].y) * (polygon[j].x - polygon[i].x) < testPoint.x)
                    {
                        result = !result;
                    }
                }
                j = i;
            }
            return result;
        }


        /// <summary> Calculates the AABB of a Polygon. </summary>
        /// <param name="polygon"> The polygon whose verticies to test. </param>
        /// <returns> Returns the bounds that encapsulate all points. </returns>
        public static Bounds GetPolygonBounds(Vector2[] polygon)
        {
            Vector2 min = MMath.Min(polygon);
            Vector2 max = MMath.Max(polygon);
            return new Bounds((min + max) / 2, max - min);
        }


        /// <summary> Calculates the shortest distance of a point to an infinite line. </summary>
        /// <param name="a"> The first point the line passes through. </param>
        /// <param name="b"> The second point the line passes through. </param>
        /// <param name="p"> The point to test the distance to. </param>
        /// <returns> Returns the shortest distance to the point. </returns>
        public static float DistancePointToLine(Vector3 a, Vector3 b, Vector3 p)
        {
            Vector3 pa = p - a, ba = b - a;
            float h = Vector3.Dot(pa, ba) / Vector3.Dot(ba, ba);
            return (pa - ba * h).magnitude;
        }
        /// <summary> Calculates the shortest distance of a point to a line segment. </summary>
        /// <param name="a"> The starting point of the line. </param>
        /// <param name="b"> The end point of the line. </param>
        /// <param name="p"> The point to test the distance to. </param>
        /// <returns> Returns the shortest distance to the point. </returns>
        public static float DistancePointToLineSegment(Vector3 a, Vector3 b, Vector3 p)
        {
            Vector3 pa = p - a, ba = b - a;
            float h = MMath.Clamp01(Vector3.Dot(pa, ba) / Vector3.Dot(ba, ba));
            return (pa - ba * h).magnitude;
        }


        /// <summary> Performs a raycast to check for a sphere. </summary>
        /// <param name="center"> The center point of the sphere. </param>
        /// <param name="radius"> The radius of the sphere. </param>
        /// <param name="ray"> The ray to use for raycasting. </param>
        /// <param name="dist"> The distance along the ray the hit occurred. -1 if no hit occurred. </param>
        /// <returns> Returns true if a hit occurred. False otherwise. </returns>
        public static bool RaycastSphere(Vector3 center, float radius, Ray ray, out float dist)
        {
            dist = -1f;
            Vector3 oc = ray.origin - center;
            float a = Vector3.Dot(ray.direction, ray.direction);
            float b = 2f * Vector3.Dot(oc, ray.direction);
            float c = Vector3.Dot(oc, oc) - radius * radius;
            float discriminant = b * b - 4f * a * c;
            if (discriminant < 0)
                return false;
            dist = (-b - MMath.Sqrt(discriminant)) / (2f * a);
            return true;
        }
    }
}