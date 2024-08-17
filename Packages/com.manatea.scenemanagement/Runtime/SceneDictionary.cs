using System;
using UnityEngine;

namespace Manatea.SceneManagement
{
    internal class SceneDictionary : ScriptableObject
    {
        [SerializeField, HideInInspector]
        internal SceneDiscription[] m_SceneList;


        [Serializable]
        internal struct SceneDiscription
        {
            public string Path;
            public string Guid;
        }
    }
}
