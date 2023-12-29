using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Manatea.CommandSystem
{
    [InitializeOnLoad]
    public static class ConsoleGUISettingsProvider
    {
        private const string SettingsPath = "Assets/Manatea/CommandSystem/ConsoleSettings.asset";
        private static ConsoleGUISettings Settings;

        private static bool styleFoldout = true;
        private static bool debugFoldout = true;
        private static bool colorFoldout = true;

        static ConsoleGUISettingsProvider()
        {
            Load();
        }

        [SettingsProvider]
        private static SettingsProvider CreateSettingsProvider()
        {
            Load();

            return new SettingsProvider("Project/Manatea/Console", SettingsScope.Project)
            {
                guiHandler = (searchContext) =>
                {
                    var settings = Load();
                    var serialized = new SerializedObject(settings);

                    EditorGUI.BeginChangeCheck();

                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(serialized.FindProperty("_consoleKey"));
                    EditorGUILayout.PropertyField(serialized.FindProperty("_modifierShift"));
                    EditorGUILayout.PropertyField(serialized.FindProperty("_modifierControl"));
                    EditorGUILayout.PropertyField(serialized.FindProperty("_modifierAlt"));
                    EditorGUILayout.PropertyField(serialized.FindProperty("_disableInBuild"));

                    EditorGUILayout.Space();
                    styleFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(styleFoldout, "Style");
                    if (styleFoldout)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(serialized.FindProperty("_font"));
                        EditorGUILayout.PropertyField(serialized.FindProperty("_defaultFontSize"));
                        EditorGUILayout.PropertyField(serialized.FindProperty("_autocompleteCommands"));
                        EditorGUILayout.PropertyField(serialized.FindProperty("_autocompleteParameters"));
                        EditorGUILayout.PropertyField(serialized.FindProperty("_showParameters"));
                        EditorGUILayout.PropertyField(serialized.FindProperty("_highlightActiveParameter"));
                        EditorGUILayout.PropertyField(serialized.FindProperty("_tintParameterTypes"));
                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.EndFoldoutHeaderGroup();

                    EditorGUILayout.Space();
                    colorFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(colorFoldout, "Colors");
                    if (colorFoldout)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(serialized.FindProperty("_colorText"));
                        EditorGUILayout.PropertyField(serialized.FindProperty("_colorBg"));
                        EditorGUILayout.PropertyField(serialized.FindProperty("_colorSelection"));
                        EditorGUILayout.Space();
                        EditorGUILayout.PropertyField(serialized.FindProperty("_paramColorBool"));
                        EditorGUILayout.PropertyField(serialized.FindProperty("_paramColorInt"));
                        EditorGUILayout.PropertyField(serialized.FindProperty("_paramColorFloat"));
                        EditorGUILayout.PropertyField(serialized.FindProperty("_paramColorEnum"));
                        EditorGUILayout.PropertyField(serialized.FindProperty("_paramColorString"));
                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.EndFoldoutHeaderGroup();

                    EditorGUILayout.Space();
                    debugFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(debugFoldout, "Debug");
                    if (debugFoldout)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(serialized.FindProperty("_hideGameObject"));
                        EditorGUILayout.PropertyField(serialized.FindProperty("_logLevel"));
                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.EndFoldoutHeaderGroup();

                    // Reset to Defaults
                    EditorGUILayout.Space();
                    if (GUILayout.Button("Reset to Defaults", GUILayout.Height(30)))
                    {
                        SerializedObject defaults = new SerializedObject(ScriptableObject.CreateInstance<ConsoleGUISettings>());

                        serialized.CopyFromSerializedProperty(defaults.FindProperty("_consoleKey"));
                        serialized.CopyFromSerializedProperty(defaults.FindProperty("_disableInBuild"));

                        serialized.CopyFromSerializedProperty(defaults.FindProperty("_font"));
                        serialized.CopyFromSerializedProperty(defaults.FindProperty("_defaultFontSize"));
                        serialized.CopyFromSerializedProperty(defaults.FindProperty("_autocompleteCommands"));
                        serialized.CopyFromSerializedProperty(defaults.FindProperty("_autocompleteParameters"));
                        serialized.CopyFromSerializedProperty(defaults.FindProperty("_showParameters"));
                        serialized.CopyFromSerializedProperty(defaults.FindProperty("_highlightActiveParameter"));
                        serialized.CopyFromSerializedProperty(defaults.FindProperty("_tintParameterTypes"));

                        serialized.CopyFromSerializedProperty(defaults.FindProperty("_colorText"));
                        serialized.CopyFromSerializedProperty(defaults.FindProperty("_colorBg"));
                        serialized.CopyFromSerializedProperty(defaults.FindProperty("_colorSelection"));
                        serialized.CopyFromSerializedProperty(defaults.FindProperty("_paramColorBool"));
                        serialized.CopyFromSerializedProperty(defaults.FindProperty("_paramColorInt"));
                        serialized.CopyFromSerializedProperty(defaults.FindProperty("_paramColorFloat"));
                        serialized.CopyFromSerializedProperty(defaults.FindProperty("_paramColorEnum"));
                        serialized.CopyFromSerializedProperty(defaults.FindProperty("_paramColorString"));

                        serialized.CopyFromSerializedProperty(defaults.FindProperty("_hideGameObject"));
                        serialized.CopyFromSerializedProperty(defaults.FindProperty("_logLevel"));
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        serialized.ApplyModifiedProperties();
                        ConsoleGUISettings.Refresh(settings);
                    }
                },
            };
        }

        public static ConsoleGUISettings Load()
        {
            if (Settings == null)
            {
                string[] assets = AssetDatabase.FindAssets("t:" + nameof(ConsoleGUISettings));
                if (assets.Length > 0)
                    Settings = AssetDatabase.LoadAssetAtPath<ConsoleGUISettings>(AssetDatabase.GUIDToAssetPath(assets[0]));
            }

            if (Settings == null)
            {
                Settings = ScriptableObject.CreateInstance<ConsoleGUISettings>();
                AssetDatabase.CreateAsset(Settings, SettingsPath);
                AssetDatabase.SaveAssets();
            }

            return Settings;
        }
    }
}