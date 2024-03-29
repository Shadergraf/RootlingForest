﻿using UnityEditor;
using UnityEngine;

namespace Manatea
{
    /// <summary>
    /// Put this attribute above any field
    /// It adds a small label to the right of the property field.
    /// This small label can be used to add information about the assumed unit.
    /// <code>
    ///     [Unit("m/s²")]
    ///     public float gravity = 9.80665f;
    /// </code>
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = true)]
    public class UnitAttribute : PropertyAttribute
    {
        public string label;
        public GUIStyle labelStyle;
        public float width;

        public UnitAttribute(string label)
        {
            this.label = label;
            labelStyle = GUI.skin.GetStyle("miniLabel");
            width = labelStyle.CalcSize(new GUIContent(label)).x;
        }
    }
}