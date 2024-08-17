using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

using Object = UnityEngine.Object;

namespace Manatea.BuildPipeline
{
    [CreateAssetMenu(menuName = "Manatea/Build Configuration")]
    public class BuildConfiguration : ScriptableObject
    {
        [SerializeField]
        private BuildTarget m_BuildTarget;
        [SerializeField]
        private BuildOptions m_BuildOptions;
        [SerializeField]
        private Object[] m_AdditionalPreloadedAssets;
        [SerializeField]
        private string[] m_PreProcessorDefines;
        [SerializeField]
        private GameSettings m_BuildGameSettings;

        public BuildTarget BuildTarget
        {
            get { return m_BuildTarget; }
            set { m_BuildTarget = value; }
        }
        public BuildOptions BuildOptions
        {
            get { return m_BuildOptions; }
            set { m_BuildOptions = value; }
        }
        public Object[] ExtraPreloadedAssets
        {
            get { return m_AdditionalPreloadedAssets; }
            set { m_AdditionalPreloadedAssets = value; }
        }
        public string[] PreProcessorDefines
        {
            get { return m_PreProcessorDefines; }
            set { m_PreProcessorDefines = value; }
        }
        public GameSettings BuildGameSettings
        {
            get { return m_BuildGameSettings; }
            set { m_BuildGameSettings = value; }
        }

        private Object[] m_Cached_PreloadedAssets;
        private string m_Cached_ScriptingDefines;
        private GameSettings m_Cached_GameSettings;


        public void Apply(ref BuildPlayerOptions buildOptions)
        {
            m_Cached_PreloadedAssets = PlayerSettings.GetPreloadedAssets();
            List<Object> newPreloadedAssets = new List<Object>(m_Cached_PreloadedAssets);
            newPreloadedAssets.AddRange(m_AdditionalPreloadedAssets);
            newPreloadedAssets.Add(BuildGameSettings);
            PlayerSettings.SetPreloadedAssets(newPreloadedAssets.ToArray());

            buildOptions.extraScriptingDefines = m_PreProcessorDefines;

            m_Cached_GameSettings = GameSettings.Current;
            GameSettings.Current = BuildGameSettings;
        }
        public void Clear()
        {
            PlayerSettings.SetPreloadedAssets(m_Cached_PreloadedAssets);
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(BuildSystem.GetBuildTargetGroupFromBuildTarget(m_BuildTarget)), m_Cached_ScriptingDefines);

            GameSettings.Current = m_Cached_GameSettings;
        }
    }
}