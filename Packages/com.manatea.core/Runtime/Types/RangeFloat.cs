using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
