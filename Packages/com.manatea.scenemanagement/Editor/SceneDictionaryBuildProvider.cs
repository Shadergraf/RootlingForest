using UnityEngine;
using UnityEditor;
using Manatea.BuildPipeline;
using Object = UnityEngine.Object;

namespace Manatea.SceneManagement
{
    public static class SceneDictionaryBuildProvider
    {
        [InitializeOnLoadMethod]
        private static void Init()
        {
            BuildSystem.RegisterPreBuildAction(PreBuildAction, 1000);
            BuildSystem.RegisterPostBuildAction(PostBuildAction, 100);
        }

        public static void PreBuildAction(ref BuildPlayerOptions options)
        {
            SetupSceneDictionary();
        }
        private static void PostBuildAction(ref BuildPlayerOptions options)
        {
            CleanupSceneDictionary();
        }


        private static void SetupSceneDictionary()
        {
            Debug.Log("Setup scene dictionary.");

            Object dict = Resources.Load("SceneDictionary");
            SerializedObject dictSO = new SerializedObject(dict);
            SerializedProperty sceneListProp = dictSO.FindProperty("m_SceneList");
            sceneListProp.ClearArray();
            foreach (var editorScene in EditorBuildSettings.scenes)
            {
                if (!editorScene.enabled)
                    continue;

                int i = sceneListProp.arraySize;
                sceneListProp.InsertArrayElementAtIndex(i);
                SerializedProperty arrayProp = sceneListProp.GetArrayElementAtIndex(i);
                arrayProp.FindPropertyRelative("Guid").stringValue = editorScene.guid.ToString();
                arrayProp.FindPropertyRelative("Path").stringValue =  editorScene.path;
            }
            dictSO.ApplyModifiedProperties();
        }

        private static void CleanupSceneDictionary()
        {
            Debug.Log("Cleanup scene dictionary.");

            Object dict = Resources.Load("SceneDictionary");
            SerializedObject dictSO = new SerializedObject(dict);
            SerializedProperty sceneListProp = dictSO.FindProperty("m_SceneList");
            sceneListProp.ClearArray();
            dictSO.ApplyModifiedProperties();
        }
    }
}
