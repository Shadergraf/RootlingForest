using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Manatea.SceneManagement
{
    public static class SceneHelper
    {
#if !UNITY_EDITOR

        private static Dictionary<string, string> m_GuidToPath = new Dictionary<string, string>();
        private static Dictionary<string, string> m_PathToGuid = new Dictionary<string, string>();


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Init()
        {
            SceneDictionary dict = Resources.Load<SceneDictionary>("SceneDictionary");
            Debug.Assert(dict, "SceneDictionary could not be loaded.");

            foreach (var sd in dict.m_SceneList)
            {
                m_GuidToPath.Add(sd.Guid, sd.Path);
                m_PathToGuid.Add(sd.Path, sd.Guid);
            }

            Debug.Log("SceneDictionary initialized.");
        }

#endif

        public static bool IsSceneValid(SceneReference sceneRef) => IsValidGuid(sceneRef.Guid) && IsValidPath(sceneRef.Guid);
        public static string GetScenePath(SceneReference sceneRef) => GuidToPath(sceneRef.Guid);

        public static SceneReference GetSceneByBuildIndex(int buildIndex)
        {
            if (buildIndex == -1)
                return default(SceneReference);
            var path = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(buildIndex);
            string guid = PathToGuid(path);
            return new SceneReference(guid);
        }
        public static SceneReference GetSceneByName(string name)
        {
            var buildIndex = UnityEngine.SceneManagement.SceneUtility.GetBuildIndexByScenePath(name);
            if (buildIndex == -1)
                return default(SceneReference);
            var path = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(buildIndex);
            string guid = PathToGuid(path);
            return new SceneReference(guid);
        }

#if UNITY_EDITOR

        public static bool IsValidGuid(string guid) => AssetDatabase.GetMainAssetTypeAtPath(AssetDatabase.GUIDToAssetPath(guid)) == typeof(SceneAsset);
        public static bool IsValidPath(string path) => AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(SceneAsset);

        public static string GuidToPath(string guid) => AssetDatabase.GUIDToAssetPath(guid);
        public static string PathToGuid(string path) => AssetDatabase.AssetPathToGUID(path);

#else

        public static bool IsValidGuid(string guid) => m_GuidToPath.ContainsKey(guid);
        public static bool IsValidPath(string path) => m_PathToGuid.ContainsKey(path);

        public static string GuidToPath(string guid) => m_GuidToPath[guid];
        public static string PathToGuid(string path) => m_PathToGuid[path];

#endif
    }
}
