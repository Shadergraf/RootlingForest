using UnityEditor;
using UnityEngine;

namespace Manatea.GameplaySystem
{
    [CustomPropertyDrawer(typeof(GameplayTagFilter))]
    public class GameplayTagFilterDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(position, property, label, property.isExpanded);

            Rect labelRect = position;
            labelRect.xMin = labelRect.xMax - 200;
            labelRect.yMax = labelRect.yMin + EditorGUIUtility.singleLineHeight;
            SerializedProperty requireListProp = property.FindPropertyRelative("RequireTags").FindPropertyRelative("m_List");
            SerializedProperty ignoreListProp = property.FindPropertyRelative("IgnoreTags").FindPropertyRelative("m_List");
            string requireCount = requireListProp.hasMultipleDifferentValues ? "-" : requireListProp.arraySize.ToString();
            string ignoreCount = ignoreListProp.hasMultipleDifferentValues ? "-" : ignoreListProp.arraySize.ToString();
            var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight };
            EditorGUI.LabelField(labelRect, requireCount + " | " + ignoreCount, style);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, property.isExpanded);
        }
    }
}