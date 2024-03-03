using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Manatea
{
    public static class MPropabilities
    {
        const MethodImplOptions INLINE = MethodImplOptions.AggressiveInlining;

        /// <summary>
        /// Returns true an average of p times per second, when this function is called in dt second intervals.
        /// </summary>
        /// <param name="p">The average propability per second. Usually between 0 and 1.</param>
        /// <param name="dt">The rate at wich this function is expected to be called.</param>
        /// <param name="randomValue">A random value between 0 and 1.</param>
        /// <returns>Returns true an average of p times per second</returns>
        [MethodImpl(INLINE)] public static bool TimedProbability(float p, float dt, float randomValue) => randomValue <= p * dt;
    }
}
