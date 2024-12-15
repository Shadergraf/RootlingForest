using Manatea;
using Manatea.RootlingForest.Navigation;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class DynamicNavMeshCollider : DynamicNavMeshObject
{
    [SerializeField]
    private Collider m_Collider;

    public override bool GetSource(out NavMeshBuildSource source)
    {
        source = new NavMeshBuildSource();
        source.area = Area;
        source.generateLinks = GenerateLinks;

        if (!m_Collider)
            return false;

        if (m_Collider is BoxCollider)
        {
            var boxCollider = m_Collider as BoxCollider;

            Vector3 center = boxCollider.transform.TransformPoint(boxCollider.center);
            Vector3 scale = boxCollider.transform.lossyScale;
            Vector3 size = Vector3.Scale(boxCollider.size, scale.Abs());

            source.shape = NavMeshBuildSourceShape.Box;
            source.transform = Matrix4x4.TRS(center, boxCollider.transform.rotation, Vector3.one);
            source.size = size;

            return true;
        }
        if (m_Collider is SphereCollider)
        {
            var sphereCollider = m_Collider as SphereCollider;

            Vector3 center = sphereCollider.transform.TransformPoint(sphereCollider.center);
            Vector3 scale = sphereCollider.transform.lossyScale;
            float radius = scale.Abs().Max() * sphereCollider.radius;

            source.shape = NavMeshBuildSourceShape.Sphere;
            source.transform = Matrix4x4.TRS(center, Quaternion.identity, Vector3.one);
            source.size = Vector3.one * radius * 2;

            return true;
        }
        if (m_Collider is CapsuleCollider)
        {
            var capsuleCollider = m_Collider as CapsuleCollider;

            Vector3 center = capsuleCollider.transform.TransformPoint(capsuleCollider.center);
            capsuleCollider.GetScaledParams(out float scaledRadius, out float scaledHeight);
            
            Quaternion rotation = capsuleCollider.transform.rotation;
            if (capsuleCollider.direction == 1)
                rotation *= Quaternion.Euler(0, 0, 90);
            if (capsuleCollider.direction == 2)
                rotation *= Quaternion.Euler(0, 90, 0);

            source.shape = NavMeshBuildSourceShape.Capsule;
            source.transform = Matrix4x4.TRS(center, rotation, new Vector3(scaledHeight, scaledRadius * 2, scaledRadius * 2));
            source.size = Vector3.one;

            return true;
        }
        if (m_Collider is MeshCollider)
        {
            var meshCollider = m_Collider as MeshCollider;

            if (!meshCollider.sharedMesh)
                return false;

            source.shape = NavMeshBuildSourceShape.Mesh;
            source.transform = meshCollider.transform.localToWorldMatrix;
            source.sourceObject = meshCollider.sharedMesh;
            source.size = Vector3.one;

            return true;
        }
        if (m_Collider is TerrainCollider)
        {
            var terrainCollider = m_Collider as TerrainCollider;

            source.shape = NavMeshBuildSourceShape.Terrain;
            source.transform = Matrix4x4.TRS(terrainCollider.transform.position, Quaternion.identity, Vector3.one);
            source.sourceObject = terrainCollider.terrainData;

            return true;
        }

        return false;
    }
}
