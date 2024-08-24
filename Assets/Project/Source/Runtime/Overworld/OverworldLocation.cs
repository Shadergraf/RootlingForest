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
        private SceneReference m_PersistentScene;


        void Awake()
        {
            string persistentScenePath = SceneHelper.GetScenePath(m_PersistentScene);
            Scene scene = SceneManager.GetSceneByPath(persistentScenePath);
            if (!scene.IsValid())
            {
                Debug.Log("PersistentScene not loaded. Attempting to load it now.");
                SceneManager.LoadScene(persistentScenePath, new LoadSceneParameters(LoadSceneMode.Additive));
            }
        }
        void Start()
        {
            Debug.Assert(OverworldManager.Instance, "No OverworldManager found!", this);
        }
    }
}
