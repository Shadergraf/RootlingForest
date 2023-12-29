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
    public static class ProjectViewUtilities
    {
        [InitializeOnLoadMethod]
        private static void Init()
        {
            EditorApplication.projectWindowItemOnGUI += ProjectWindowItemOnGUI;
        }

        private static void ProjectWindowItemOnGUI(string guid, Rect selectionRect)
        {
            FocusByLetter(guid);
        }

        private static void FocusByLetter(string guid)
        {
            if (Event.current.type != EventType.KeyDown)
                return;
            if (Event.current.alt)
                return;
            if (Event.current.control)
                return;
            if (Event.current.shift)
                return;
            if (EditorGUIUtility.editingTextField)
                return;

            // Check for single character keypress
            string key = Event.current.keyCode.ToString();
            if (key.Length > 1)
                return;

            string folderPath = GetCurrentOpenProjectFolder();
            string path = Application.dataPath + folderPath.Remove(0, 6);

            // Gather and filter asset files
            List<string> matchingFiles = new List<string>();
            matchingFiles.AddRange(Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly));
            matchingFiles.AddRange(Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly));
            matchingFiles = matchingFiles.ConvertAll(p => p.Replace(path, "").Remove(0, 1));
            matchingFiles.RemoveAll(p => p.EndsWith(".meta"));
            matchingFiles.RemoveAll(p => !p.StartsWith(key));
            if (matchingFiles.Count == 0)
                return;

            // Get current selection
            int targetSelection = 0;
            if (Selection.count == 1)
            {
                string selectionPath = AssetDatabase.GetAssetPath(Selection.activeObject);
                if (!string.IsNullOrEmpty(selectionPath))
                {
                    string selectionName = selectionPath.Replace(folderPath, "").Remove(0, 1);
                    targetSelection = matchingFiles.IndexOf(selectionName) + 1;
                    targetSelection = (int)MMath.Repeat(targetSelection, matchingFiles.Count);
                }
            }

            // Select target asset
            string assetPath = folderPath + "\\" + matchingFiles[targetSelection].Replace(Application.dataPath, "Assets");
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            Event.current.Use();
        }

        public static string GetCurrentOpenProjectFolder()
        {
            Type projectWindowUtilType = typeof(ProjectWindowUtil);
            MethodInfo getActiveFolderPath = projectWindowUtilType.GetMethod("GetActiveFolderPath", BindingFlags.Static | BindingFlags.NonPublic);
            object obj = getActiveFolderPath.Invoke(null, new object[0]);
            string pathToCurrentFolder = obj.ToString();
            return pathToCurrentFolder;
        }
    }
}
