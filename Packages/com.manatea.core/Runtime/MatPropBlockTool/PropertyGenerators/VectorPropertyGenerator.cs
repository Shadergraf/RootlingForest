using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Manatea.MaterialPropertyTool
{
    [CreateAssetMenu(fileName = "VectorPropertyGenerator", menuName = "Manatea/Rendering/MatPropBlock/Basic Vector Property Generator")]
    public class VectorPropertyGenerator : PropertyGenerator<Vector4>
    {
        public Vector4 minValue;

        public Vector4 maxValue;

        public override Vector4 GeneratePropertyValue(int seed)
        {
            var rng = new System.Random(seed);

            return Vector4.Lerp(minValue, maxValue, (float)rng.NextDouble());
        }
    }

}