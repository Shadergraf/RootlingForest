using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Manatea.SceneManagement;
using UnityEngine.SceneManagement;
using System.Security.Cryptography;

namespace Manatea.RootlingForest
{
    public class GameMode : MonoBehaviour
    {
        [SerializeField]
        private SceneReference m_GameModeScene;


        void Awake()
        {
            string persistentScenePath = SceneHelper.GetScenePath(m_GameModeScene);
            Scene scene = SceneManager.GetSceneByPath(persistentScenePath);
            if (!scene.IsValid())
            {
                Debug.Log("GameModeScene not loaded. Attempting to load it now.");
                SceneManager.LoadScene(persistentScenePath, new LoadSceneParameters(LoadSceneMode.Additive));
            }
        }
        void Start()
        {
            Debug.Assert(GameModeManager.Instance, "No GameModeManager found!", this);

            GameModeManager.Instance.RegisterGameMode(this);
        }

        private void OnDisable()
        {
            // Due to order of shutdown we can not be sure that the GameModeManager still exists at this point. So we need to check first
            if (GameModeManager.Instance)
            {
                GameModeManager.Instance.UnregisterGameMode();
            }
        }
    }
}
