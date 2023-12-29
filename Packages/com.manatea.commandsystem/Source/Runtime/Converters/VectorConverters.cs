using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using UnityEngine;
using System.Globalization;

namespace Manatea.CommandSystem.Converters
{
    public abstract class VectorConverter : TypeConverter
    {
        public override bool IsValid(ITypeDescriptorContext context, object value)
        {
            if (value is string)
            {
                try
                {
                    string str = (string)value;

                    // Check for wrapping parentheses
                    if (!str.StartsWith("(") || !str.EndsWith(")"))
                        return false;

                    // Check for only one pair of parentheses
                    if (str.Count(f => f == '(') != 1 || str.Count(f => f == ')') != 1)
                        return false;

                    // Check for number of numeric values
                    string[] numbers = GetParsedNumbers(str);
                    if (numbers.Length != GetElementCount())
                        return false;

                    // Ensure valid numbers throuout the array
                    foreach (string num in numbers)
                        if (!float.TryParse(num, NumberStyles.Float, CultureInfo.InvariantCulture, out float o))
                            return false;

                    return true;
                }
                catch
                {
                    return false;
                }

            }
            return base.IsValid(context, value);
        }
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
            {
                return true;
            }
            return base.CanConvertFrom(context, sourceType);
        }


        protected abstract int GetElementCount();

        protected string[] GetParsedNumbers(string value)
        {
            // Remove spaces
            value.Replace(" ", "");

            // Remove the parentheses
            if (value.StartsWith("(") && value.EndsWith(")"))
            {
                value = value.Substring(1, value.Length - 2);
            }

            // split the items
            return value.Split(CultureInfo.InvariantCulture.NumberFormat.NumberGroupSeparator[0]);
        }

        protected string ParseNumbers(float[] numbers)
        {
            string output = "(";
            for (int i = 0; i < numbers.Length; i++)
            {
                if (i != 0)
                    output += CultureInfo.InvariantCulture.NumberFormat.NumberGroupSeparator;
                output += numbers[i].ToString(CultureInfo.InvariantCulture);
            }
            output += ")";
            return output;
        }
    }


    public class Vector2Converter : VectorConverter
    {
        protected override int GetElementCount() => 2;

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string)
            {
                string[] values = GetParsedNumbers((string)value);
                Vector2 result = new Vector2(
                    float.Parse(values[0], CultureInfo.InvariantCulture),
                    float.Parse(values[1], CultureInfo.InvariantCulture));

                return result;
            }
            return base.ConvertFrom(context, CultureInfo.InvariantCulture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                Vector2 vec = (Vector2)value;
                return ParseNumbers(new float[] { vec.x, vec.y });
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
    public class Vector3Converter : VectorConverter
    {
        protected override int GetElementCount() => 3;

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string)
            {
                string[] values = GetParsedNumbers((string)value);
                Vector3 result = new Vector3(
                    float.Parse(values[0], CultureInfo.InvariantCulture),
                    float.Parse(values[1], CultureInfo.InvariantCulture),
                    float.Parse(values[2], CultureInfo.InvariantCulture));

                return result;
            }
            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                Vector3 vec = (Vector3)value;
                return ParseNumbers(new float[] { vec.x, vec.y, vec.z });
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
    public class Vector4Converter : VectorConverter
    {
        protected override int GetElementCount() => 4;

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string)
            {
                string[] values = GetParsedNumbers((string)value);
                Vector4 result = new Vector4(
                    float.Parse(values[0], culture),
                    float.Parse(values[1], culture),
                    float.Parse(values[2], culture),
                    float.Parse(values[3], culture));

                return result;
            }
            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                Vector4 vec = (Vector4)value;
                return ParseNumbers(new float[] { vec.x, vec.y, vec.z, vec.w });
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
