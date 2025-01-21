using UnityEngine;
using UnityEditor;

namespace Manatea
{
    [CustomPropertyDrawer(typeof(Fetched<>))]
    public class FetchedPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var valueProperty = property.FindPropertyRelative("value");
            return EditorGUI.GetPropertyHeight(valueProperty);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var valueProperty = property.FindPropertyRelative("value");
            var fetchingTypeProperty = property.FindPropertyRelative("fetchingType");

            EditorGUI.BeginProperty(position, label, property);

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            Rect typeRect = position;
            typeRect.x += typeRect.width;
            typeRect.width = 90;
            typeRect.x -= typeRect.width;
            EditorGUI.PropertyField(typeRect, fetchingTypeProperty, GUIContent.none);
            EditorGUI.indentLevel = indent;

            Rect valueRect = position;
            valueRect.width -= 90;
            if (fetchingTypeProperty.enumValueIndex == 0)
            {
                EditorGUI.PropertyField(valueRect, valueProperty, label, true);
            }
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.PropertyField(valueRect, valueProperty, label, true);
                EditorGUI.EndDisabledGroup();
            }

            EditorGUI.EndProperty();
        }
    }
}
