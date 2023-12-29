using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine
{
    public static class TransformExtensions
    {


        /// <summary>
        /// Destroys all children of this transform.
        /// </summary>
        public static void DestroyAllChildren(this Transform transform)
        {
            foreach (var child in GetAllChildren(transform))
            {
                Transform.Destroy(child.gameObject);
            }
        }

        /// <summary>
        /// Destroys all children of this transform, but immediately.
        /// </summary>
        public static void DestroyAllChildrenImmediate(this Transform transform)
        {
            foreach (var child in GetAllChildren(transform))
            {
                Transform.DestroyImmediate(child.gameObject);
            }
        }




        private static List<Transform> GetAllChildren(this Transform transform)
        {
            List<Transform> children = new List<Transform>();

            for (int i = 0; i < transform.childCount; i++)
            {
                children.Add(transform.GetChild(i));
            }

            return children;
        }

    }
}

