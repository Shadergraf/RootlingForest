using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

namespace Manatea.BuildPipeline
{
    class GameSettingsProvider : SettingsProvider
    {
        private Editor currentEditor;


        [SettingsProvider]
        public static SettingsProvider CreateFromSettingsFromFunctor()
        {
            return new GameSettingsProvider();
        }

        public GameSettingsProvider()
            : base("Project/Game Settings", SettingsScope.Project)
        { }


        public override void OnGUI(string searchContext)
        {
            var boldtext = new GUIStyle(GUI.skin.label);
            boldtext.fontStyle = FontStyle.Bold;
            GUILayout.Label("Game Settings", boldtext);
            GameSettings newGameSettings = (GameSettings)EditorGUILayout.ObjectField(GameSettings.Current, typeof(GameSettings), true);
            if (newGameSettings != GameSettings.Current)
            {
                GameSettings.Current = newGameSettings;
            }
            EditorGUILayout.Space(20);

            if (GameSettings.Current)
            {
                if (currentEditor == null || currentEditor.target != GameSettings.Current)
                {
                    currentEditor = Editor.CreateEditor(GameSettings.Current);
                }

                currentEditor.OnInspectorGUI();
            }
        }
    }
}
