using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace Manatea.GameplaySystem
{
    [CustomPropertyDrawer(typeof(GameplayTagCollection))]
    public class GameplayTagCollectionDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var relative = property.FindPropertyRelative("m_List");
            EditorGUI.PropertyField(position, relative, label, true);
        }
    
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var relative = property.FindPropertyRelative("m_List");
            return EditorGUI.GetPropertyHeight(relative, label, true);
        }
    }
}
