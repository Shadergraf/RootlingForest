using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Manatea.SceneManagement;
using UnityEngine.SceneManagement;
using System.Security.Cryptography;

namespace Manatea.RootlingForest
{
    public class OverworldLocation : MonoBehaviour
    {
        [SerializeField]
        private SceneReference m_OverworldScene;


        void Awake()
        {
            string scenePath = SceneHelper.GetScenePath(m_OverworldScene);
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            if (!scene.IsValid())
            {
                Debug.Log("OverworldScene not loaded. Attempting to load it now.");
                SceneManager.LoadScene(scenePath, new LoadSceneParameters(LoadSceneMode.Additive));
            }
        }
        void Start()
        {
            Debug.Assert(OverworldManager.Instance, "No OverworldManager found!", this);
        }
    }
}
