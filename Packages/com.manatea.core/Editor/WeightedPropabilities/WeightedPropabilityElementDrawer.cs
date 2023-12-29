using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Manatea;

namespace Manatea
{
    [CustomPropertyDrawer(typeof(WeightedPropabilitySpace<>.WeightedPropabilityElement<>))]
    public class WeightedPropabilityElementDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PrefixLabel(position, label);
            Rect labelRect = EditorGUI.PrefixLabel(position, label);
            Rect dataRect = position;
            dataRect.xMin += labelRect.width / 2;
            Rect widthRect = dataRect;
            dataRect.xMax -= 50;
            widthRect.xMin += dataRect.width;
            EditorGUI.PropertyField(dataRect, property.FindPropertyRelative("Data"), GUIContent.none);
            EditorGUI.PropertyField(widthRect, property.FindPropertyRelative("Weight"), GUIContent.none);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(SerializedPropertyType.Generic, label);
        }
    }
}
