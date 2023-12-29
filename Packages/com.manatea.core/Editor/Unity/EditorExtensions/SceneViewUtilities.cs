using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Manatea.Unity
{
    public static class SceneViewUtilities
    {
        [InitializeOnLoadMethod]
        private static void Init()
        {
            SceneView.duringSceneGui += DuringSceneGui;
            EditorApplication.hierarchyWindowItemOnGUI += HierarchyWindowItemOnGUI;
            EditorApplication.projectWindowItemOnGUI += ProjectWindowItemOnGUI;
        }

        private static void DuringSceneGui(SceneView scene)
        {
            DeselectOnEscape();
        }
        private static void HierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
        {
            DeselectOnEscape();
        }
        private static void ProjectWindowItemOnGUI(string guid, Rect selectionRect)
        {
            DeselectOnEscape();
        }

        private static void DeselectOnEscape()
        {
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
                Selection.activeObject = null;
        }
    }
}
