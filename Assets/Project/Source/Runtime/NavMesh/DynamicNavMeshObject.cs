using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace Manatea.RootlingForest.Navigation
{
    [DefaultExecutionOrder(-200)]
    public abstract class DynamicNavMeshObject : MonoBehaviour
    {
        [SerializeField]
        private int m_Area;
        [SerializeField]
        private bool m_GenerateLinks = true;

        public int Area => m_Area;
        public bool GenerateLinks => m_GenerateLinks;


        protected void OnEnable()
        {
            DynamicNavMeshManager.AddObject(this);
        }
        protected void OnDisable()
        {
            DynamicNavMeshManager.RemoveObject(this);
        }

        public abstract bool GetSource(out NavMeshBuildSource source);
    }
}
