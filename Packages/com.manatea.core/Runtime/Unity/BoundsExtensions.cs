using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Manatea.Unity
{
    public static class BoundsExtensions
    {
        /// <summary>
        /// Calculate all corners for these bounds. No particular order
        /// </summary>
        public static Vector3[] GetCorners(this Bounds bounds)
        {
            return new Vector3[] 
            { 
                new Vector3(bounds.min.x, bounds.min.y, bounds.min.z),
                new Vector3(bounds.min.x, bounds.min.y, bounds.max.z),
                new Vector3(bounds.min.x, bounds.max.y, bounds.min.z),
                new Vector3(bounds.min.x, bounds.max.y, bounds.max.z),

                new Vector3(bounds.max.x, bounds.min.y, bounds.min.z),
                new Vector3(bounds.max.x, bounds.min.y, bounds.max.z),
                new Vector3(bounds.max.x, bounds.max.y, bounds.min.z),
                new Vector3(bounds.max.x, bounds.max.y, bounds.max.z)
            };
        }
    }
}

