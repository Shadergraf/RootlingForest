using UnityEditor;
using UnityEngine;

namespace Manatea
{
    [CustomPropertyDrawer(typeof(UnitAttribute))]
    public class UnitAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            UnitAttribute labelAttribute = attribute as UnitAttribute;
            
            Rect labelRect = position;
            labelRect.xMin = labelRect.xMax - labelAttribute.width - 2f;
            position.xMax -= labelRect.width;
            
            EditorGUI.PropertyField(position, property, label);
            GUI.Label(labelRect, labelAttribute.label, labelAttribute.labelStyle);
        }
    }
}