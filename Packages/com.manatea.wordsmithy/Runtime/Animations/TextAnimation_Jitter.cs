using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;

namespace Manatea.WordSmithy.Animations
{
    [CreateAssetMenu(menuName = CreateAssetPath + "Jitter")]
    public class TextAnimation_Jitter : TextAnimation
    {
        [SerializeField]
        public float FrameRate = 30;
        [SerializeField]
        public float Strength = 0.25f;
        [SerializeField]
        public Vector2 AspectRatio = Vector2.one;
        [SerializeField, Range(0, 1)]
        public float Percentage = 1;


        public override bool Animate(ref TMP_CharacterInfo charInfo, ref TextAnimationData data, ref TMP_MeshInfo meshInfo, ref CharacterExtraData extraData)
        {
            Vector2 offset = Vector2.zero;
            if (Random.value < Percentage)
            {
                Random.InitState((int)Noise((int)(data.time * FrameRate) * (extraData.Index + 1)));
                offset = Random.insideUnitCircle;

                // Apply aspect ratio scaling
                offset.x *= AspectRatio.x;
                offset.y *= AspectRatio.y;

                // Apply strength and point size
                float mult = 0.5f * Strength * charInfo.pointSize;
                offset.x *= mult;
                offset.y *= mult;
            }

            // Fast vertex offset
            meshInfo.vertices[charInfo.vertexIndex + 0].x += offset.x;
            meshInfo.vertices[charInfo.vertexIndex + 0].y += offset.y;
            meshInfo.vertices[charInfo.vertexIndex + 1].x += offset.x;
            meshInfo.vertices[charInfo.vertexIndex + 1].y += offset.y;
            meshInfo.vertices[charInfo.vertexIndex + 2].x += offset.x;
            meshInfo.vertices[charInfo.vertexIndex + 2].y += offset.y;
            meshInfo.vertices[charInfo.vertexIndex + 3].x += offset.x;
            meshInfo.vertices[charInfo.vertexIndex + 3].y += offset.y;

            return true;
        }

        private float Noise(float x)
        {
            return MMath.Frac(MMath.Sin(78.233f * x)) * 43758.5453f;
        }
    }
}
