using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

namespace Manatea.GameplaySystem
{
    [CustomEditor(typeof(GameplayTagOwner))]
    [CanEditMultipleObjects]
    public class GameplayTagOwnerInspector : Editor
    {
        private SerializedProperty m_PermanentTags;
        private SerializedProperty m_TemporaryTags;

        private void OnEnable()
        {
            m_PermanentTags = serializedObject.FindProperty("m_InitialPermanentTags");
            m_TemporaryTags = serializedObject.FindProperty("m_InitialTemporaryTags");
        }

        public override void OnInspectorGUI()
        {
            var tagOwner = (GameplayTagOwner)target;
            if (tagOwner.didAwake)
                DrawPlayingGUI();
            else
                DrawEditorGUI();

            serializedObject.ApplyModifiedProperties();
            serializedObject.UpdateIfRequiredOrScript();
        }

        private void DrawEditorGUI()
        {
            EditorGUILayout.PropertyField(m_PermanentTags);
            EditorGUILayout.PropertyField(m_TemporaryTags);
        }
        private void DrawPlayingGUI()
        {
            EditorGUILayout.LabelField("Current tags:", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
            GUI.enabled = false;

            var tagOwner = (GameplayTagOwner)target;
            var tags = tagOwner.GetAllTags();
            for (int i = 0; i < tags.Count; i++)
            {
                EditorGUILayout.ObjectField(tags[i], typeof(GameplayTag), false);
            }

            GUI.enabled = true;
            EditorGUI.indentLevel--;
        }
    }
}
