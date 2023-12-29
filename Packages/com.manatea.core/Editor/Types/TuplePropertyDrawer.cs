using System;
using System.Reflection;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;

namespace Manatea
{
    // TODO give bool, float, int fields some smaller default width to look cleaner in the editor

    [CustomPropertyDrawer(typeof(Tuple2<,>))]
    public class Tuple2PropertyDrawer : PropertyDrawer
    {
        private const int k_ElementCount = 2;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Rect dataRect = EditorGUI.PrefixLabel(position, label);
            for (int i = 0; i < k_ElementCount; i++)
            {
                Rect propertyRect = dataRect;
                propertyRect.width = dataRect.width / k_ElementCount;
                propertyRect.x += i * propertyRect.width;
                EditorGUI.PropertyField(propertyRect, property.FindPropertyRelative("Element" + (i + 1).ToString()), GUIContent.none);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(SerializedPropertyType.Generic, label);
        }
    }
    [CustomPropertyDrawer(typeof(Tuple3<,,>))]
    public class Tuple3PropertyDrawer : PropertyDrawer
    {
        private const int k_ElementCount = 3;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Rect dataRect = EditorGUI.PrefixLabel(position, label);
            for (int i = 0; i < k_ElementCount; i++)
            {
                Rect propertyRect = dataRect;
                propertyRect.width = dataRect.width / k_ElementCount;
                propertyRect.x += i * propertyRect.width;
                EditorGUI.PropertyField(propertyRect, property.FindPropertyRelative("Element" + (i + 1).ToString()), GUIContent.none);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(SerializedPropertyType.Generic, label);
        }
    }
    [CustomPropertyDrawer(typeof(Tuple4<,,,>))]
    public class Tuple4PropertyDrawer : PropertyDrawer
    {
        private const int k_ElementCount = 4;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Rect dataRect = EditorGUI.PrefixLabel(position, label);
            for (int i = 0; i < k_ElementCount; i++)
            {
                Rect propertyRect = dataRect;
                propertyRect.width = dataRect.width / k_ElementCount;
                propertyRect.x += i * propertyRect.width;
                EditorGUI.PropertyField(propertyRect, property.FindPropertyRelative("Element" + (i + 1).ToString()), GUIContent.none);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(SerializedPropertyType.Generic, label);
        }
    }
}
