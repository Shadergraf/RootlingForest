using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Manatea.RootlingForest.Navigation
{
    [RequireComponent(typeof(NavMeshSurface))]
    public class NavMeshSurfaceUpdater : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The period at which to update the NavMesh. A value of 0 means updating the NavMesh every frame. A negative value prevents the NavMesh from updating.")]
        private float m_UpdatePeriod = -1;
        [SerializeField]
        private float m_InitialDelay = 0;
        [SerializeField]
        private Bounds m_Bounds = new Bounds(Vector3.zero, Vector3.one * 10);
        [SerializeField]
        private bool m_AutoUpdateBounds;
        [SerializeField]
        private LayerMask m_LayerMask;

        private NavMeshSurface m_NavMeshSurface;
        private Bounds m_CurrentBounds;

        private YieldInstruction m_YieldWait_InitialDelay;
        private YieldInstruction m_YieldWait_UpdatePeriod;


        private void Awake()
        {
            m_NavMeshSurface = GetComponent<NavMeshSurface>();

            m_YieldWait_InitialDelay = new WaitForSeconds(m_InitialDelay);
            m_YieldWait_UpdatePeriod = new WaitForSeconds(m_UpdatePeriod);
        }
        private void OnEnable()
        {
            StartCoroutine(TickNavMesh());
        }
        private void OnDisable()
        {
            StopAllCoroutines();
        }


        private IEnumerator TickNavMesh()
        {
            UpdateNavMesh();

            yield return m_YieldWait_InitialDelay;
            
            while (true)
            {
                if (m_UpdatePeriod >= 0)
                {
                    UpdateNavMesh();
                }

                yield return m_YieldWait_UpdatePeriod;
            }
        }

        private void UpdateNavMesh()
        {
            //DynamicNavMeshManager.SyncNavMeshSources();
            //var sources = DynamicNavMeshManager.GetNavMeshSources();

            //m_CurrentBounds = m_Bounds;
            //if (m_AutoUpdateBounds)
            //    m_CurrentBounds = DynamicNavMeshManager.ComputeBounds(transform, sources);

            //var settings = m_NavMeshSurface.GetBuildSettings();

            //var markups = new List<NavMeshBuildMarkup>();
            //markups.Add(new NavMeshBuildMarkup()
            //{
            //    applyToChildren = true,
            //    area = 1,
            //    generateLinks = true,
            //    root = m_NavMeshSurface.transform,
            //});

            //sources.Clear();
            //var sources = new List<NavMeshBuildSource>();

            //NavMeshBuilder.CollectSources(m_CurrentBounds, m_LayerMask, NavMeshCollectGeometry.PhysicsColliders, 0, markups, sources);
            //NavMeshBuilder.UpdateNavMeshData(m_NavMeshSurface.navMeshData, settings, sources, m_CurrentBounds);
            m_NavMeshSurface.UpdateNavMesh(m_NavMeshSurface.navMeshData);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(m_CurrentBounds.center, m_CurrentBounds.size);
        }
    }
}
