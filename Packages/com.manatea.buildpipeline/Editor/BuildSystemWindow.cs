using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Manatea.BuildPipeline
{
    public class BuildSystemWindow : EditorWindow
    {
        private BuildConfiguration m_CurrentBuildConfiguration;
        private BuildOptions m_AdditionalBuildOptions;

        [SerializeField, HideInInspector]
        private string s_BuildId;


        [MenuItem("Manatea/Build System")]
        static void Init()
        {
            BuildSystemWindow window = (BuildSystemWindow)EditorWindow.GetWindow(typeof(BuildSystemWindow));
            window.titleContent = new GUIContent("Build System");
            window.Show();
            window.UpdateBuildId();
        }

        private void OnGUI()
        {
            GUILayout.Space(8);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Build Version:");
            GUILayout.Label(s_BuildId);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(EditorGUIUtility.IconContent("d_RotateTool On")))
                UpdateBuildId();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(16);

            // Edit current build config
            m_CurrentBuildConfiguration = (BuildConfiguration)EditorGUILayout.ObjectField(new GUIContent("Current Build Configuration"), m_CurrentBuildConfiguration, typeof(BuildConfiguration), false);

            // Edit additional build options
            m_AdditionalBuildOptions = (BuildOptions)EditorGUILayout.EnumFlagsField(new GUIContent("Current Build Configuration"), m_AdditionalBuildOptions);

            GUILayout.Space(16);
            
            // Kickoff build
            if (GUILayout.Button("Kickoff build for current configuration"))
            {
                BuildSystem.RunEditorBuild(m_CurrentBuildConfiguration, m_AdditionalBuildOptions);
            }
        }

        private void BuildConfigListGUI()
        {

        }


        private void UpdateBuildId()
        {
            BuildSystem.GetVersionAndRevisionFromGit(out string version, out string revision);
            if (revision.Length > 8)
                revision = revision.Remove(8, revision.Length - 8);
            s_BuildId = version + " - " + revision;
        }
    }
}
