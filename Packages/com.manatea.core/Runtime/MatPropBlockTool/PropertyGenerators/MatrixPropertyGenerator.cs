using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Manatea.MaterialPropertyTool
{
    [CreateAssetMenu(fileName = "MatrixPropertyGenerator", menuName = "Manatea/Rendering/MatPropBlock/Basic Matrix Property Generator")]
    public class MatrixPropertyGenerator : PropertyGenerator<Matrix4x4>
    {
        public Matrix4x4 minValue;

        public Matrix4x4 maxValue;

        public override Matrix4x4 GeneratePropertyValue(int seed)
        {
            var rng = new System.Random(seed);
            var r = (float)rng.NextDouble();

            var matrix = minValue;

            for (int row = 0; row <= 4; row++)
            {
                for (int column = 0; column <= 4; column++)
                {
                    matrix[row, column] += maxValue[row, column] * r;
                }
            }

            return matrix;
        }
    }

}