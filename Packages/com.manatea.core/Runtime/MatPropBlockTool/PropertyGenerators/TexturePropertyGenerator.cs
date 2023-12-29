using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Manatea.MaterialPropertyTool
{
    [CreateAssetMenu(fileName = "TexturePropertyGenerator", menuName = "Manatea/Rendering/MatPropBlock/Basic Texture Property Generator")]
    public class TexturePropertyGenerator : PropertyGenerator<Texture2D>
    {
        public Texture2D[] texturePool;

        public override Texture2D GeneratePropertyValue(int seed)
        {
            var rng = new System.Random(seed);

            return texturePool[rng.Next(0, texturePool.Length - 1)] ;
        }
    }

}