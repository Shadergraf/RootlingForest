using UnityEngine;

namespace Manatea
{
    public static class MGUI
    {
        private static Texture2D s_Pixel;

        private static void Validate()
        {
            if (s_Pixel == null)
            {
                s_Pixel = new Texture2D(1, 1);
                s_Pixel.SetPixel(0, 0, Color.white);
                s_Pixel.Apply();
            }
        }

        public static void DrawScreenProgressBar(Rect screenRect, float progress)
        {
            Validate();

            GUI.DrawTexture(screenRect, s_Pixel, ScaleMode.StretchToFill, true, 0, new Color(64, 0, 0), 0, 0);
            screenRect.width *= MMath.Clamp01(progress);
            GUI.DrawTexture(screenRect, s_Pixel, ScaleMode.StretchToFill, true, 0, new Color(0, 128, 0), 0, 0);
        }
        public static void DrawWorldProgressBar(Vector3 worldPos, Rect screenRect, float progress)
        {
            Validate();

            Vector3 screenPoint = Camera.main.WorldToScreenPoint(worldPos);
            screenRect.x = screenPoint.x - screenRect.x;
            screenRect.y = Screen.height - screenPoint.y - screenRect.y;
            DrawScreenProgressBar(screenRect, progress);
        }
    }
}