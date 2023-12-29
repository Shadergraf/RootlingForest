using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Manatea.CommandSystem
{
    public static class DebugCommands
    {
        [Command(parameterAutocompleteDelegates: new string[] { nameof(ATC_SceneName) })]
        public static void LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }

        [Command(parameterAutocompleteDelegates: new string[] { nameof(ATC_SceneName) })]
        public static void LoadSceneAdditive(string sceneName)
        {
            SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
        }

        private static string[] ATC_SceneName()
        {
            List<string> paths = new List<string>();
            List<string> levels = new List<string>();
            List<string> output = new List<string>();

            // Fetch all scenes
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string sceneName = SceneUtility.GetScenePathByBuildIndex(i);
                paths.Add(sceneName);
                levels.Add(sceneName.Replace(".unity", "").Remove(0, sceneName.LastIndexOf('/') + 1));
            }

            // Check doubled scenes
            for (int i = 0; i < levels.Count; i++)
            {
                if (levels.FindAll(l => levels[i] == l).Count == 1)
                {
                    if (paths.FindIndex(p => p.Contains(levels[i] + ".unity")) != -1)
                        output.Add(levels[i]);
                }
                else
                {
                    output.Add(paths[i]);
                }
            }

            output.Sort();
            return output.ToArray();
        }

        [Command]
        public static void ToggleCursor()
        {
            Cursor.visible = !Cursor.visible;
        }

        [Command]
        public static void SetCursorLock(CursorLockMode lockMode)
        {
            Cursor.lockState = lockMode;
        }

#if UNITY_EDITOR
        [Command]
        public static void PausePlayMode()
        {
            Debug.Break();
        }
#endif
    }
}
