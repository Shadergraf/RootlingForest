using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Manatea.BuildPipeline
{
    public abstract class GameSettings : ScriptableObject
    {
        [SerializeField]
        private string m_GameName = "";
        public static string GameName => Current.m_GameName;


#if UNITY_EDITOR
        private const string kConfigKey = "manatea.buildsystem.gamesettings";
#endif

        private static GameSettings s_Current;
        public static GameSettings Current
        {
            get
            {
#if UNITY_EDITOR
                if (!s_Current)
                {
                    EditorBuildSettings.TryGetConfigObject(kConfigKey, out s_Current);
                }
#endif
                return s_Current;
            }
#if UNITY_EDITOR
            set
            {
                if (value != null)
                {
                    value.MakeCurrent();
                }
                else
                {
                    s_Current = null;
                    EditorBuildSettings.RemoveConfigObject(kConfigKey);
                }
            }
#endif
        }


        protected virtual void OnEnable()
        {
#if !UNITY_EDITOR
            if (s_Current != null)
            {
                Debug.LogError("No more than one GameSettings object can be setup!");
                return;
            }
            
            s_Current = this;
            Debug.Log("Loaded " + this.GetType().Name + ".");
#endif
        }


#if UNITY_EDITOR
        private void MakeCurrent()
        {
            s_Current = this;
            EditorBuildSettings.AddConfigObject(kConfigKey, s_Current, true);
        }
        public bool IsCurrent
        {
            get
            {
                return EditorBuildSettings.TryGetConfigObject(kConfigKey, out GameSettings game) &&
                    game == this;
            }
        }
#endif
    }
}
