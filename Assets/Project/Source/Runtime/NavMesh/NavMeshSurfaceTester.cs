using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshSurfaceTester : MonoBehaviour
{
    [SerializeField]
    private NavMeshSurface m_NavMeshSurface;
    [SerializeField]
    private Transform m_TargetTransform;
    [SerializeField]
    private Collider[] m_Collider;

    NavMeshData m_NavMeshData;
    NavMeshBuildSettings m_BuildSettigns;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        m_BuildSettigns = NavMesh.GetSettingsByIndex(2);

        var sources = CollectSources();

        //m_NavMeshData = NavMeshBuilder.BuildNavMeshData(m_BuildSettigns, sources, new Bounds(Vector3.zero, Vector3.one * 50), transform.position, transform.rotation);

        StartCoroutine(UpdateNavMesh());
    }

    private IEnumerator UpdateNavMesh()
    {
        m_NavMeshSurface.RemoveData();



        yield return new WaitForSeconds(1);

        m_NavMeshSurface.AddData();


        //var source = new NavMeshBuildSource();
        //source.shape = NavMeshBuildSourceShape.
        //sources.Add()

        while (true)
        {
            //yield return new WaitForSeconds(0.1f);
            yield return new WaitForEndOfFrame();

            //m_NavMeshSurface.BuildNavMesh();

            var settings = NavMesh.GetSettingsByIndex(2);
            settings.overrideTileSize = true;
            settings.tileSize = 64;
            Debug.Log(settings.tileSize);
            //settings.overrideTileSize = true;

            var sources = CollectSources();
            //NavMeshBuilder.UpdateNavMeshData(m_NavMeshSurface.navMeshData, settings, sources, new Bounds(m_TargetTransform.position, Vector3.one * Random.Range(7, 13)));
            NavMeshBuilder.UpdateNavMeshData(m_NavMeshSurface.navMeshData, m_NavMeshSurface.GetBuildSettings(), sources, new Bounds(Vector3.zero, Vector3.one * Random.Range(30, 40)));

            //m_NavMeshSurface.UpdateNavMesh(m_NavMeshSurface.navMeshData);

            //NavMeshBuilder.UpdateNavMeshData(m_NavMeshData, m_BuildSettigns, sources, new Bounds(Vector3.zero, Vector3.one * 10));

        }
    }

    // Update is called once per frame
    //private void Update()
    //{
    //    var settings = NavMesh.GetSettingsByIndex(2);
    //    settings.overrideTileSize = m_TileSize == -1;
    //    settings.tileSize = m_TileSize;
    //    m_NavMeshSurface.UpdateNavMesh(m_NavMeshSurface.navMeshData);
    //
    //    //m_NavMeshSurface.BuildNavMesh();
    //
    //}

    private List<NavMeshBuildSource> CollectSources()
    {
        List<NavMeshBuildSource> sources = new List<NavMeshBuildSource>();
        for (int i = 0; i < m_Collider.Length; i++)
        {
            if (m_Collider[i] is MeshCollider)
            {
                sources.Add(new NavMeshBuildSource() { area = 3, shape = NavMeshBuildSourceShape.Mesh, sourceObject = (m_Collider[i] as MeshCollider).sharedMesh, transform = m_Collider[i].transform.localToWorldMatrix });
            }
            if (m_Collider[i] is SphereCollider)
            {
                //sources.Add(new NavMeshBuildSource() { area = 2, shape = NavMeshBuildSourceShape.Sphere, size = m_Collider[i].transform.localScale * (m_Collider[i] as SphereCollider).radius * 2, transform = m_Collider[i].transform.localToWorldMatrix });
                sources.Add(new NavMeshBuildSource() { area = 3, shape = NavMeshBuildSourceShape.ModifierBox, size = Vector3.one * 2, transform = Matrix4x4.TRS(m_Collider[i].transform.position, Quaternion.identity, Vector3.one) });
            }
            if (m_Collider[i] is BoxCollider)
            {
                sources.Add(new NavMeshBuildSource() { area = 0, shape = NavMeshBuildSourceShape.Box, size = (m_Collider[i] as BoxCollider).size, transform = m_Collider[i].transform.localToWorldMatrix });
            }
        }

        return sources;
    }
}
