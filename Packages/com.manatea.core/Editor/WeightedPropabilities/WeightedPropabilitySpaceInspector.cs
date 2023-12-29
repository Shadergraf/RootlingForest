using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Manatea
{
    [CustomPropertyDrawer(typeof(WeightedPropabilitySpace<>))]
    public class WeightedPropabilitySpaceInspector : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(position, property.FindPropertyRelative("m_Propabilities"), label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property.FindPropertyRelative("m_Propabilities"), label, true);
        }
    }
}
