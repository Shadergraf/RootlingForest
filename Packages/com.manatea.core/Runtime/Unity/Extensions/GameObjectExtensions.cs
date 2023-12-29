#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
#endif

namespace UnityEngine
{
    public static class GameObjectExtensions
    {
#if UNITY_EDITOR
        public static bool IsInPlayingScene(this GameObject gameObject) =>
            EditorApplication.isPlaying &&
            !PrefabUtility.IsPartOfPrefabAsset(gameObject) &&
            !(StageUtility.GetStage(gameObject) is PrefabStage);
#else
        public static bool IsInPlayingScene(this GameObject gameObject) => true;
#endif
    }
}
