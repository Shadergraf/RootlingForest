using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Manatea.MaterialPropertyTool
{
    [CreateAssetMenu(fileName = "IntPropertyGenerator", menuName = "Manatea/Rendering/MatPropBlock/Basic Int Property Generator")]
    public class IntPropertyGenerator : PropertyGenerator<int>
    {
        public int minValue;

        public int maxValue;

        public override int GeneratePropertyValue(int seed)
        {
            var rng = new System.Random(seed);

            return rng.Next(minValue, maxValue);
        }
    }

}