using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.CompilerServices;

namespace Manatea
{
    // All functions taken from Inigo Quilez Blog
    // https://www.iquilezles.org/www/articles/distgradfunctions2d/distgradfunctions2d.htm

    public static class SDG2
    {
        const MethodImplOptions INLINE = MethodImplOptions.AggressiveInlining;

        [MethodImpl(INLINE)]
        public static Vector3 Circle(Vector2 p, float r)
        {
            float d = p.magnitude;
            Vector2 pd = p / d;
            return new Vector3(d - r, pd.x, pd.y);
        }

        [MethodImpl(INLINE)]
        public static Vector3 Box(Vector2 p, Vector2 b)
        {
            Vector2 w = MMath.Abs(p) - b;
            Vector2 s = new Vector2(p.x < 0.0 ? -1 : 1, p.y < 0.0 ? -1 : 1);
            float g = MMath.Max(w.x, w.y);
            Vector2 q = MMath.Max(w, Vector2.zero);
            float l = q.magnitude;
            Vector2 pd = s * ((g > 0.0) ? q / l : ((w.x > w.y) ? new Vector2(1, 0) : new Vector2(0, 1)));
            return new Vector3((g > 0.0) ? l : g, pd.x, pd.y);
        }

        [MethodImpl(INLINE)]
        public static Vector3 Min(Vector3 a, Vector3 b)
        {
            return (a.x < b.x) ? a : b;
        }

        [MethodImpl(INLINE)]
        public static Vector3 SmoothMin(Vector3 a, Vector3 b, float k)
        {
            float h = MMath.Max(k - MMath.Abs(a.x - b.x), 0.0f);
            float m = 0.25f * h * h / k;
            float n = 0.50f * h / k;
            Vector2 pd = Vector2.Lerp(a.YZ(), b.YZ(), (a.x < b.x) ? n : 1.0f - n);
            return new Vector3(MMath.Min(a.x, b.x) - m, pd.x, pd.y);
        }
    }
}
