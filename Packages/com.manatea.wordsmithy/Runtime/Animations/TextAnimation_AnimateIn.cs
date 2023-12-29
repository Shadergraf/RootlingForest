using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;

namespace Manatea.WordSmithy.Animations
{
    [CreateAssetMenu(menuName = CreateAssetPath + "Animate In")]
    public class TextAnimation_AnimateIn : TextAnimation
    {
        [SerializeField]
        public float Time = 0.25f;
        [SerializeField]
        public Vector2 Offset = Vector2.zero;
        [SerializeField]
        public float Rotation = 0;
        [SerializeField]
        public Vector2 Scale = Vector2.one;
        [SerializeField]
        public Vector2 Pivot = Vector2.one * 0.5f;
        [SerializeField]
        public Color Tint = new Color(1, 1, 1, 0);


        public override bool Animate(ref TMP_CharacterInfo charInfo, ref TextAnimationData data, ref TMP_MeshInfo meshInfo, ref CharacterExtraData extraData)
        {
            // Calculate animation time
            float t = Mathf.Clamp01(extraData.TimeSinceVisibilityChange / Time);

            // Optimize out finished animations (t = 1)
            if (t == 1)
                return false;

            // Smoothing
            t = MMath.Smooth01(t);

            // Fetch mesh info
            int vertexIndex = charInfo.vertexIndex;
            Vector3[] verticies = meshInfo.vertices;

            // Get character pivot
            Vector3 pivot = Vector3.zero;
            pivot.x = MMath.Lerp(charInfo.topLeft.x, charInfo.bottomRight.x, Pivot.x);
            pivot.y = MMath.Lerp(charInfo.topLeft.y, charInfo.bottomRight.y, Pivot.y);

            // Modify vertex positions
            Vector2 offset = Offset;
            float offsetScale = 0.5f * charInfo.pointSize * (1 - t);
            offset.x *= offsetScale;
            offset.y *= offsetScale;

            #region Pivot

            if (pivot.x != 0)
            {
                verticies[vertexIndex + 0].x -= pivot.x;
                verticies[vertexIndex + 1].x -= pivot.x;
                verticies[vertexIndex + 2].x -= pivot.x;
                verticies[vertexIndex + 3].x -= pivot.x;
            }
            if (pivot.y != 0)
            {
                verticies[vertexIndex + 0].y -= pivot.y;
                verticies[vertexIndex + 1].y -= pivot.y;
                verticies[vertexIndex + 2].y -= pivot.y;
                verticies[vertexIndex + 3].y -= pivot.y;
            }

            #endregion

            #region Rotation

            if (Rotation % 360 != 0)
            {
                float sin = Mathf.Sin(Rotation * Mathf.Deg2Rad * (1 - t));
                float cos = Mathf.Cos(Rotation * Mathf.Deg2Rad * (1 - t));

                float tx, ty;

                tx = verticies[vertexIndex + 0].x;
                ty = verticies[vertexIndex + 0].y;
                verticies[vertexIndex + 0].x = (cos * tx) - (sin * ty);
                verticies[vertexIndex + 0].y = (sin * tx) + (cos * ty);

                tx = verticies[vertexIndex + 1].x;
                ty = verticies[vertexIndex + 1].y;
                verticies[vertexIndex + 1].x = (cos * tx) - (sin * ty);
                verticies[vertexIndex + 1].y = (sin * tx) + (cos * ty);

                tx = verticies[vertexIndex + 2].x;
                ty = verticies[vertexIndex + 2].y;
                verticies[vertexIndex + 2].x = (cos * tx) - (sin * ty);
                verticies[vertexIndex + 2].y = (sin * tx) + (cos * ty);

                tx = verticies[vertexIndex + 3].x;
                ty = verticies[vertexIndex + 3].y;
                verticies[vertexIndex + 3].x = (cos * tx) - (sin * ty);
                verticies[vertexIndex + 3].y = (sin * tx) + (cos * ty);
            }

            #endregion

            #region Scale

            if (Scale.x != 0)
            {
                float scaleX = MMath.Lerp(Scale.x, 1, t);
                verticies[vertexIndex + 0].x *= scaleX;
                verticies[vertexIndex + 1].x *= scaleX;
                verticies[vertexIndex + 2].x *= scaleX;
                verticies[vertexIndex + 3].x *= scaleX;
            }
            if (Scale.y != 0)
            {
                float scaleY = MMath.Lerp(Scale.y, 1, t);
                verticies[vertexIndex + 0].y *= scaleY;
                verticies[vertexIndex + 1].y *= scaleY;
                verticies[vertexIndex + 2].y *= scaleY;
                verticies[vertexIndex + 3].y *= scaleY;
            }

            #endregion

            #region Offset and pivot

            float opX = offset.x * (1 - t) + pivot.x;
            if (opX != 0)
            {
                verticies[vertexIndex + 0].x += opX;
                verticies[vertexIndex + 1].x += opX;
                verticies[vertexIndex + 2].x += opX;
                verticies[vertexIndex + 3].x += opX;
            }
            float opY = offset.y * (1 - t) + pivot.y;
            if (opY != 0)
            {
                verticies[vertexIndex + 0].y += opY;
                verticies[vertexIndex + 1].y += opY;
                verticies[vertexIndex + 2].y += opY;
                verticies[vertexIndex + 3].y += opY;
            }

            #endregion

            #region Vertex colors

            Color tint = Color.Lerp(Tint, Color.white, t);
            Color32[] colors = meshInfo.colors32;
            if (tint.r != 1)
            {
                colors[vertexIndex + 0].r = (byte)(colors[vertexIndex + 0].r * tint.r);
                colors[vertexIndex + 1].r = (byte)(colors[vertexIndex + 1].r * tint.r);
                colors[vertexIndex + 2].r = (byte)(colors[vertexIndex + 2].r * tint.r);
                colors[vertexIndex + 3].r = (byte)(colors[vertexIndex + 3].r * tint.r);
            }
            if (tint.g != 1)
            {
                colors[vertexIndex + 0].g = (byte)(colors[vertexIndex + 0].g * tint.g);
                colors[vertexIndex + 1].g = (byte)(colors[vertexIndex + 1].g * tint.g);
                colors[vertexIndex + 2].g = (byte)(colors[vertexIndex + 2].g * tint.g);
                colors[vertexIndex + 3].g = (byte)(colors[vertexIndex + 3].g * tint.g);
            }
            if (tint.b != 1)
            {
                colors[vertexIndex + 0].b = (byte)(colors[vertexIndex + 0].b * tint.b);
                colors[vertexIndex + 1].b = (byte)(colors[vertexIndex + 1].b * tint.b);
                colors[vertexIndex + 2].b = (byte)(colors[vertexIndex + 2].b * tint.b);
                colors[vertexIndex + 3].b = (byte)(colors[vertexIndex + 3].b * tint.b);
            }
            if (tint.a != 1)
            {
                colors[vertexIndex + 0].a = (byte)(colors[vertexIndex + 0].a * tint.a);
                colors[vertexIndex + 1].a = (byte)(colors[vertexIndex + 1].a * tint.a);
                colors[vertexIndex + 2].a = (byte)(colors[vertexIndex + 2].a * tint.a);
                colors[vertexIndex + 3].a = (byte)(colors[vertexIndex + 3].a * tint.a);
            }

            #endregion

            return true;
        }
    }
}
