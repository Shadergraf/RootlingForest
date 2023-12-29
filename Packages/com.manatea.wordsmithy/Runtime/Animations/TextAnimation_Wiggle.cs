using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;

namespace Manatea.WordSmithy.Animations
{
    [CreateAssetMenu(menuName = CreateAssetPath + "Wiggle")]
    public class TextAnimation_Wiggle : TextAnimation
    {
        [SerializeField]
        public float Speed = 20;
        [SerializeField]
        public float CharacterOffset = 1;
        [SerializeField]
        public float Strength = 0.25f;
        [SerializeField]
        public Vector2 AspectRatio = Vector2.one;
        [SerializeField]
        public Vector2 Offset;


        public override bool Animate(ref TMP_CharacterInfo charInfo, ref TextAnimationData data, ref TMP_MeshInfo meshInfo, ref CharacterExtraData extraData)
        {
            float time = data.time * Speed + extraData.Index * CharacterOffset;
            Vector2 offset = new Vector2(MMath.Sin(time), MMath.Cos(time));

            // Apply aspect ratio scaling
            offset.x *= AspectRatio.x;
            offset.y *= AspectRatio.y;

            // Apply strength and point size
            float mult = 0.5f * Strength * charInfo.pointSize;
            offset.x *= mult;
            offset.y *= mult;

            // Overall offset
            offset.x += Offset.x;
            offset.y += Offset.y;

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
    }
}
