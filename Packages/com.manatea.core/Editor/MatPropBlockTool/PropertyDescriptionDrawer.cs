using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace Manatea.MaterialPropertyTool
{
    [CustomPropertyDrawer(typeof(MaterialPropertyDescription<>), true)]
    public class PropertyDescriptionDrawer : PropertyDrawer
    {

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var valueHeightoffset = 0.0F;

            var propType = property.FindPropertyRelative("propertyType");

            if (propType.enumValueIndex == 0) // constant
            {
                valueHeightoffset = EditorGUI.GetPropertyHeight(property.FindPropertyRelative("constValue"));
            }
            else // generator
            {
                valueHeightoffset = EditorGUIUtility.singleLineHeight;
            }

            return base.GetPropertyHeight(property, label) * 3 + valueHeightoffset;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            {
                var nameRect = new Rect(position.min + (EditorGUIUtility.singleLineHeight * 0.5F * Vector2.up), new Vector2(position.size.x, EditorGUIUtility.singleLineHeight));

                var typeRect = new Rect(nameRect.position + ((EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * Vector2.up), nameRect.size);

                var valueRect = new Rect();

                valueRect.min = new Vector2(typeRect.min.x, typeRect.max.y + EditorGUIUtility.standardVerticalSpacing);
                valueRect.max = position.max - new Vector2(0, EditorGUIUtility.singleLineHeight / 4);


                EditorGUI.PropertyField(nameRect, property.FindPropertyRelative("name"));


                var propType = property.FindPropertyRelative("propertyType");


                EditorGUI.PropertyField(typeRect, propType);


                if (propType.enumValueIndex == 0) // constant
                {
                    EditorGUI.PropertyField(valueRect, property.FindPropertyRelative("constValue"));
                }
                else // generator
                {
                    EditorGUI.PropertyField(valueRect, property.FindPropertyRelative("generator"));
                }

                EditorGUI.PropertyField(nameRect, property.FindPropertyRelative("name"));

            }
            EditorGUI.EndProperty();

        }

    }

}