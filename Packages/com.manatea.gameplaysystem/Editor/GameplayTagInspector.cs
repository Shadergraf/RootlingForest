using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

namespace Manatea.GameplaySystem
{
    [CustomEditor(typeof(GameplayTag))]
    [CanEditMultipleObjects]
    public class GameplayTagInspector : Editor
    {
        private SerializedProperty m_Description;
        private SerializedProperty m_ParentProperty;
        private SerializedProperty m_AncestorCountProperty;

        private void OnEnable()
        {
            m_Description = serializedObject.FindProperty("m_Description");
            m_ParentProperty = serializedObject.FindProperty("m_Parent");
            m_AncestorCountProperty = serializedObject.FindProperty("AncestorCount");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(m_Description);
            EditorGUILayout.PropertyField(m_ParentProperty);

            GUI.enabled = false;
            if (targets.Length == 1)
            {
                EditorGUILayout.IntField("Ancestor Count", (target as GameplayTag).AncestorCount);
            }
            else
            {
                EditorGUI.showMixedValue = true;
                EditorGUILayout.IntField("Ancestor Count", 0);
                EditorGUI.showMixedValue = false;
            }
            GUI.enabled = true;

            serializedObject.ApplyModifiedProperties();
            serializedObject.UpdateIfRequiredOrScript();
        }
    }
}
