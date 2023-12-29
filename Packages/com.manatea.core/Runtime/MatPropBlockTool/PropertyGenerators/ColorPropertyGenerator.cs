using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Manatea.MaterialPropertyTool
{
    [CreateAssetMenu(fileName = "ColorPropertyGenerator", menuName = "Manatea/Rendering/MatPropBlock/Basic Color Property Generator")]
    public class ColorPropertyGenerator : PropertyGenerator<Color>
    {
        public Gradient gradient = new Gradient();

        public override Color GeneratePropertyValue(int seed)
        {
            if(gradient == null)
            {
                gradient = new Gradient();
            }

            var rng = new System.Random(seed);

            var time = (float)rng.NextDouble();

            return gradient.Evaluate(time);
        }
    }
     
}