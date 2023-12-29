using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityEditor
{
    /**
    * This class is a "fake", global, Editor: it jumps into memory once, and sits
    * in the background globally listening to selections, so that it can patch
    * Unity's broken (and officially: they won't fix it) implementation of [SelectionBase]
    *
    * Based on @StunAustralia's code at: https://forum.unity.com/threads/in-editor-select-the-parent-instead-of-an-object-in-the-messy-hierarchy-it-creates.543479/#post-3586524
    * Updated to fix issues with drill-down. This version: https://forum.unity.com/threads/in-editor-select-the-parent-instead-of-an-object-in-the-messy-hierarchy-it-creates.543479/#post-5691667
*/
    [InitializeOnLoad]
    public class FixUnityBrokenSelectionBase : Editor
    {
        private static List<UnityEngine.Object> newSelection = null;
        private static UnityEngine.Object[] lastSelection = new UnityEngine.Object[] { };
        static FixUnityBrokenSelectionBase()
        {
            // Ensure we're told when the selection changes
            Selection.selectionChanged += OnSelectionChanged;
            // For some reason I can't be bothered investigating, you can't modify selections
            // while in OnSelectionChanged() so... hack to do it in Update() instead
            EditorApplication.update += OnSceneUpdate;
        }
        public static void OnSelectionChanged()
        {
            if (SceneView.mouseOverWindow == null)
                return;

            // Only modify user selection if selected from the SceneView
            System.Type windowOver = SceneView.mouseOverWindow.GetType();
            System.Type sceneView = typeof(SceneView);
            if (!windowOver.Equals(sceneView)) return;


            //  Look through them all, adjusting as needed
            var futureSeletion = new List<UnityEngine.Object>();
            bool changed = false;
            foreach (GameObject go in Selection.GetFiltered<GameObject>(SelectionMode.Unfiltered))
            {
                changed = changed | AdjustIfNeeded(go, lastSelection, futureSeletion);
            }
            // If nothing has changed, give the update nothing to reselect
            if (!changed)
            {
                futureSeletion = null;
            }

            /** Only update newSelection atomically */
            newSelection = futureSeletion;
            // Remember this selection so we can compare the next selection to it
            lastSelection = Selection.objects;
        }
        private static bool AdjustIfNeeded(GameObject go, object[] lastSelection, List<UnityEngine.Object> newSelection)
        {
            //Debug.Log("Selected: "+go );

            // If it was in the last selection set, leave it be
            if (Array.IndexOf(lastSelection, go) < 0)
            {
                //Debug.Log("...wasn't selected");

                GameObject parentWithGlobalSelectionBase = null;
                bool goHasGlobalSelectionBase = ObjectHasGlobalSelectionBase(go);
                parentWithGlobalSelectionBase = ParentWithGlobalSelectionBase(go);
                if
                (
                   parentWithGlobalSelectionBase != null
                   && (Array.IndexOf(lastSelection, parentWithGlobalSelectionBase) < 0)
                   && (Array.IndexOf(lastSelection, go.transform.parent.gameObject) < 0)
                )
                {
                    //Debug.Log("....user NOT drilling down");
                    // User NOT drilling down - replace selection with GlobalSelectionBase parent
                    newSelection.Add(parentWithGlobalSelectionBase.gameObject);

                    return true;
                }
            }
            newSelection.Add(go);   // original go
            return false;
        }
        public static void OnSceneUpdate()
        {
            if (newSelection != null)
            {
                Selection.objects = newSelection.ToArray();
                newSelection = null;
            }
        }
        public static bool ObjectHasGlobalSelectionBase(GameObject go)
        {
            foreach (Component component in go.GetComponents<MonoBehaviour>())
            {
                if (component.GetType().GetCustomAttributes(typeof(GlobalSelectionBase), true).Length > 0)
                {
                    return true;
                }
            }
            return false;
        }
        public static GameObject ParentWithGlobalSelectionBase(GameObject go)
        {
            if (go.transform.parent == null) return null;
            foreach (Component component in go.transform.parent.GetComponentsInParent<MonoBehaviour>(false))
            {
                if (component.GetType().GetCustomAttributes(typeof(GlobalSelectionBase), true).Length > 0)
                {
                    return component.gameObject;
                }
            }
            return null;
        }
    }
}
