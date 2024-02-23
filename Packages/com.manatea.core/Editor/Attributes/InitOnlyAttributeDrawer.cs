using UnityEditor;
using UnityEngine;

namespace Manatea
{
    [CustomPropertyDrawer(typeof(InitOnlyAttribute))]
    public class InitOnlyAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginDisabledGroup(property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue != null);
            EditorGUI.PropertyField(position, property, label, true);
            EditorGUI.EndDisabledGroup();
        }
    }
}