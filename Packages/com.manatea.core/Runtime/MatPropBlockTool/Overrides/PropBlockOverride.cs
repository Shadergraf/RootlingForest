using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Manatea.MaterialPropertyTool
{
    [System.Serializable]
    public class PropBlockOverride
    {
        public enum PropertyTargetType { Color, Float, Int, Matrix, Texture, Vector };
        public enum PropertyMethod { Set, Add, Subtract, Multiply};

        [SerializeField]
        public string targetProperty = "_";

        [SerializeField][Range(0,1)]
        public float influence = 1;

        [SerializeField]
        public PropertyTargetType propertyTargetType = PropertyTargetType.Color;

        [SerializeField]
        public PropertyMethod method = PropertyMethod.Set;


        [ColorUsage(true, true)]
        public Color colorValue = Color.white;

        public float floatValue = 1;

        public int intValue = 1;

        public Matrix4x4 matrixValue;

        public Texture2D textureValue;

        public Vector4 vectorValue;

        public Color GetOverrideValue(Color original)
        {
            switch (method)
            {
                case PropertyMethod.Set:
                    return Color.Lerp(original, colorValue, influence);
                case PropertyMethod.Add:
                    return original + (influence * colorValue);
                case PropertyMethod.Subtract:
                    return original - (influence * colorValue);
                case PropertyMethod.Multiply:
                    return Color.Lerp(original, original * colorValue, influence);
                default:
                    return colorValue;
            }
        }

        public float GetOverrideValue(float original)
        {
            switch (method)
            {
                case PropertyMethod.Set:
                    return Mathf.Lerp(original, floatValue, influence);
                case PropertyMethod.Add:
                    return original + (influence * floatValue);
                case PropertyMethod.Subtract:
                    return original - (influence * floatValue);
                case PropertyMethod.Multiply:
                    return Mathf.Lerp(original, original * floatValue, influence);
                default:
                    return floatValue;
            }
        }


        public int GetOverrideValue(int original)
        {
            switch (method)
            {
                case PropertyMethod.Set:
                    return Mathf.RoundToInt(Mathf.Lerp(original, intValue, influence));
                case PropertyMethod.Add:
                    return original + Mathf.RoundToInt(influence * intValue);
                case PropertyMethod.Subtract:
                    return original - Mathf.RoundToInt(influence * intValue);
                case PropertyMethod.Multiply:
                    return Mathf.RoundToInt(Mathf.Lerp(original, original * intValue, influence));
                default:
                    return intValue;
            }
        }

        public Matrix4x4 GetOverrideValue(Matrix4x4 original)
        {
            switch (method)
            {
                case PropertyMethod.Set:
                    var lerp = original;

                    for (int row = 0; row < 4; row++)
                    {
                        for (int column = 0; column < 4; column++)
                        {
                            lerp[row, column] = Mathf.Lerp(lerp[row, column], matrixValue[row, column], influence);
                        }
                    }

                    return lerp;
                //case PropertyMethod.Add:

                //    var sum = original;

                //    for (int row = 0; row < 4; row++)
                //    {
                //        for (int column = 0; column < 4; column++)
                //        {
                //            sum[row, column] += (influence * matrixValue[row, column]);
                //        }
                //    }

                //    return sum;
                //case PropertyMethod.Subtract:

                //    var dif = original;

                //    for (int row = 0; row < 4; row++)
                //    {
                //        for (int column = 0; column < 4; column++)
                //        {
                //            dif[row, column] -= (influence * matrixValue[row, column]);
                //        }
                //    }

                //    return dif;
                case PropertyMethod.Multiply:

                    var mul = original * matrixValue;

                    for (int row = 0; row < 4; row++)
                    {
                        for (int column = 0; column < 4; column++)
                        {
                            mul[row, column] = Mathf.Lerp(original[row, column], mul[row, column], influence);
                        }
                    }

                    return mul;
                default:
                    return matrixValue;
            }
        }

        public Texture2D GetOverrideValue(Texture2D original)
        {
            switch (method)
            {
                default:
                    return textureValue;
            }
        }

        public Vector4 GetOverrideValue(Vector4 original)
        {
            switch (method)
            {
                case PropertyMethod.Set:
                    return (Vector4.Lerp(original, vectorValue, influence));
                case PropertyMethod.Add:
                    return original + (influence * vectorValue);
                case PropertyMethod.Subtract:
                    return original - (influence * vectorValue);
                case PropertyMethod.Multiply:

                    var mul = original;

                    mul.x *= vectorValue.x;
                    mul.y *= vectorValue.y;
                    mul.z *= vectorValue.z;
                    mul.w *= vectorValue.w;


                    return (Vector4.Lerp(original, mul, influence));
                default:
                    return vectorValue;
            }
        }

    }
}
