using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Manatea.Unity
{
    internal static class DeselectOnEscapeUtility
    {
        [InitializeOnLoadMethod]
        private static void Init()
        {
            SceneView.duringSceneGui += DuringSceneGui;
            EditorApplication.hierarchyWindowItemOnGUI += HierarchyWindowItemOnGUI;
            EditorApplication.projectWindowItemOnGUI += ProjectWindowItemOnGUI;
        }

        private static void DuringSceneGui(SceneView scene) => HandleUtility();
        private static void HierarchyWindowItemOnGUI(int instanceID, Rect selectionRect) => HandleUtility();
        private static void ProjectWindowItemOnGUI(string guid, Rect selectionRect) => HandleUtility();

        private static void HandleUtility()
        {
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
                Selection.activeObject = null;
        }
    }
}
