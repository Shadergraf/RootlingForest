using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;

namespace Manatea.SceneManagement
{
    [CustomPropertyDrawer(typeof(SceneReference))]
    public class SceneRefDrawer : PropertyDrawer
    {
        private string m_CurrentGuid;
        private SceneAsset m_CurrentScene;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            string guid = property.FindPropertyRelative("m_Guid").stringValue;
            if (guid != m_CurrentGuid)
            {
                m_CurrentGuid = guid;
                m_CurrentScene = (SceneAsset)AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(m_CurrentGuid));
            }

            SceneAsset newScene = (SceneAsset)EditorGUI.ObjectField(position, label, m_CurrentScene, typeof(SceneAsset), false);
            if (newScene != m_CurrentScene)
                property.FindPropertyRelative("m_Guid").stringValue = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(newScene));

            EditorGUI.EndProperty();
        }
    }
}
