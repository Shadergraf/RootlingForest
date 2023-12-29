using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Manatea.MaterialPropertyTool
{
    [CreateAssetMenu(fileName = "FloatPropertyGenerator", menuName = "Manatea/Rendering/MatPropBlock/Basic Float Property Generator")]
    public class FloatPropertyGenerator : PropertyGenerator<float>
    {
        public float minValue;

        public float maxValue;

        public override float GeneratePropertyValue(int seed)
        {
            var rng = new System.Random(seed);

            return Mathf.Lerp(minValue, maxValue, (float)rng.NextDouble());
        }
    }

}