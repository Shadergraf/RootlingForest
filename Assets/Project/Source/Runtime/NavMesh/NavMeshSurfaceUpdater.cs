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

        private NavMeshSurface m_NavMeshSurface;

        private YieldInstruction m_YieldWait_InitialDelay;
        private YieldInstruction m_YieldWait_UpdatePeriod;


        private void Awake()
        {
            m_NavMeshSurface = GetComponent<NavMeshSurface>();
            m_NavMeshSurface.RemoveData();
            m_NavMeshSurface.navMeshData = Instantiate(m_NavMeshSurface.navMeshData);
            m_NavMeshSurface.AddData();

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
            // TODO what if this takes longer to update than the tick frequency? Investigate!
            m_NavMeshSurface.UpdateNavMesh(m_NavMeshSurface.navMeshData);
        }
    }
}
