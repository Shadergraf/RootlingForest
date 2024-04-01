using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Manatea
{

    [System.Serializable]
    public struct RangeFloat
    {
        /// <summary>
        /// The starting value of the range
        /// </summary>
        public float start;

        /// <summary>
        /// The length of the range
        /// </summary>
        public float length;

        /// <summary>
        /// The end value of the range
        /// </summary>
        public float end => start + length;

        /// <summary>
        /// Constructs a new RangeFloat with given start, length values
        /// </summary>
        /// <param name="start">The starting value of the range</param>
        /// <param name="length">The length of the range</param>
        public RangeFloat(float start, float length)
        {
            this.start = start;
            this.length = length;
        }

        /// <summary>
        /// Tests if the provided value inside the range, excluding the range borders.
        /// </summary>
        /// <param name="value">The value to test.</param>
        /// <returns>Whether the value is exclusively in range.</returns>
        public bool IsInsideRangeExclusive(float value) => value > start && value < end;
        /// <summary>
        /// Tests if the provided value inside the range, including the range borders.
        /// </summary>
        /// <param name="value">The value to test.</param>
        /// <returns>Whether the value is inclusively in range.</returns>
        public bool IsInsideRangeInclusive(float value) => value > start && value < end;

        /// <summary>
        /// Clamps a value to the range borders.
        /// </summary>
        /// <param name="value">The value to clamp.</param>
        /// <returns>The value clamped to the provided.</returns>
        public float ClampValueToRange(float value) => MMath.Clamp(value, start, end);
    }
}
