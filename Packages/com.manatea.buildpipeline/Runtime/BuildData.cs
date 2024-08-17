using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Manatea.BuildPipeline
{
    internal class BuildData : ScriptableObject
    {
        [SerializeField]
        private string m_BuildSha;

        public string BuildSha
        {
            get { return m_BuildSha; }
            set { m_BuildSha = value; }
        }
    }
}
