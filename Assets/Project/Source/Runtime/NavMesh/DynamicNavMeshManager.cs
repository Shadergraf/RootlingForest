using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Manatea.RootlingForest.Navigation
{
    public static class DynamicNavMeshManager
    {
        private static List<DynamicNavMeshObject> s_NavMeshObjs = new List<DynamicNavMeshObject>();

        private static List<NavMeshBuildSource> s_NavMeshSources;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            s_NavMeshObjs = new List<DynamicNavMeshObject>();
            s_NavMeshSources = new List<NavMeshBuildSource>();
        }

        public static void AddObject(DynamicNavMeshObject obj)
        {
            s_NavMeshObjs.Add(obj);
        }
        public static void RemoveObject(DynamicNavMeshObject obj)
        {
            s_NavMeshObjs.Remove(obj);
        }


        public static void SyncNavMeshSources()
        {
            s_NavMeshSources.Clear();
            foreach (var obj in s_NavMeshObjs)
            {
                bool success = obj.GetSource(out var source);
                if (!success)
                {
                    source.shape = NavMeshBuildSourceShape.Sphere;
                    source.size = Vector3.zero;
                }
                s_NavMeshSources.Add(source);
            }
        }
        public static List<NavMeshBuildSource> GetNavMeshSources()
        {
            return s_NavMeshSources;
        }

        // From NavMeshSurface.cs
        public static Bounds ComputeBounds(Transform transform, List<NavMeshBuildSource> sources)
        {
            // Use the unscaled matrix for the NavMeshSurface
            Matrix4x4 worldToLocal = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            worldToLocal = worldToLocal.inverse;

            var result = new Bounds();
            foreach (var src in sources)
            {
                switch (src.shape)
                {
                    case NavMeshBuildSourceShape.Mesh:
                        {
                            var m = src.sourceObject as Mesh;
                            result.Encapsulate(GetWorldBounds(worldToLocal * src.transform, m.bounds));
                            break;
                        }
                    case NavMeshBuildSourceShape.Terrain:
                        {
                            // Terrain pivot is lower/left corner - shift bounds accordingly
                            var t = src.sourceObject as TerrainData;
                            result.Encapsulate(GetWorldBounds(worldToLocal * src.transform, new Bounds(0.5f * t.size, t.size)));
                            break;
                        }
                    case NavMeshBuildSourceShape.Box:
                    case NavMeshBuildSourceShape.Sphere:
                    case NavMeshBuildSourceShape.Capsule:
                    case NavMeshBuildSourceShape.ModifierBox:
                        result.Encapsulate(GetWorldBounds(worldToLocal * src.transform, new Bounds(Vector3.zero, src.size)));
                        break;
                }
            }
            // Inflate the bounds a bit to avoid clipping co-planar sources
            result.Expand(0.1f);
            return result;
        }
        private static Bounds GetWorldBounds(Matrix4x4 mat, Bounds bounds)
        {
            var absAxisX = mat.MultiplyVector(Vector3.right).Abs();
            var absAxisY = mat.MultiplyVector(Vector3.up).Abs();
            var absAxisZ = mat.MultiplyVector(Vector3.forward).Abs();
            var worldPosition = mat.MultiplyPoint(bounds.center);
            var worldSize = absAxisX * bounds.size.x + absAxisY * bounds.size.y + absAxisZ * bounds.size.z;
            return new Bounds(worldPosition, worldSize);
        }
    }
}
