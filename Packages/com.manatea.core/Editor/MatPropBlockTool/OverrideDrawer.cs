using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace Manatea.MaterialPropertyTool
{
    [CustomPropertyDrawer(typeof(PropBlockOverride))]
    public class OverrideDrawer : PropertyDrawer
    {

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var propType = property.FindPropertyRelative("propertyTargetType");

            var valueHeightoffset = 0.0F;

            switch (propType.enumValueIndex)
            {
                case 0: //color
                    valueHeightoffset = EditorGUI.GetPropertyHeight(property.FindPropertyRelative("colorValue"));
                    break;
                case 1: //float
                    valueHeightoffset = EditorGUI.GetPropertyHeight(property.FindPropertyRelative("floatValue"));
                    break;
                case 2: //int
                    valueHeightoffset = EditorGUI.GetPropertyHeight(property.FindPropertyRelative("intValue"));
                    break;
                case 3: //matrix
                    valueHeightoffset = EditorGUI.GetPropertyHeight(property.FindPropertyRelative("matrixValue"));
                    break;
                case 4: //texture
                    valueHeightoffset = EditorGUI.GetPropertyHeight(property.FindPropertyRelative("textureValue"));
                    break;
                case 5: //vector
                    valueHeightoffset = EditorGUI.GetPropertyHeight(property.FindPropertyRelative("vectorValue"));
                    break;
            }


            return base.GetPropertyHeight(property, label) * 5 + valueHeightoffset;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            {
                var propType = property.FindPropertyRelative("propertyTargetType");

                var nameRect = new Rect(position.min + (EditorGUIUtility.singleLineHeight * 0.5F * Vector2.up), new Vector2(position.size.x, EditorGUIUtility.singleLineHeight));

                var typeRect = new Rect(nameRect.position + ((EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * Vector2.up), nameRect.size);

                var methodRect = new Rect(typeRect.position + ((EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * Vector2.up), typeRect.size);

                var influenceRect = new Rect(methodRect.position + ((EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * Vector2.up), methodRect.size);

                var valueRect = new Rect();

                valueRect.min = new Vector2(influenceRect.min.x, influenceRect.max.y + EditorGUIUtility.standardVerticalSpacing);
                valueRect.max = position.max - new Vector2(0, EditorGUIUtility.singleLineHeight / 4);


                EditorGUI.PropertyField(nameRect, property.FindPropertyRelative("targetProperty"));

                EditorGUI.PropertyField(typeRect, property.FindPropertyRelative("propertyTargetType"));

                EditorGUI.PropertyField(methodRect, property.FindPropertyRelative("method"));

                if(property.FindPropertyRelative("propertyTargetType").enumValueIndex == 3) //do some filtering for certain matrix operations
                {
                    var method = property.FindPropertyRelative("method").enumValueIndex;

                    if (method == 1 || method == 2) //add or subtract mode
                    {
                        GUI.Box(influenceRect, GUIContent.none, EditorStyles.helpBox);
                        EditorGUI.LabelField(influenceRect, "This operation is undefined! Using Set Mode...", EditorStyles.centeredGreyMiniLabel);
                    }
                    else
                    {
                        EditorGUI.PropertyField(influenceRect, property.FindPropertyRelative("influence"));
                    }
                }
                else if(property.FindPropertyRelative("propertyTargetType").enumValueIndex == 4)
                {
                    GUI.Box(influenceRect, GUIContent.none, EditorStyles.helpBox);
                    EditorGUI.LabelField(influenceRect, "No mixing supported, just overrides!", EditorStyles.centeredGreyMiniLabel);
                }
                else
                {
                    EditorGUI.PropertyField(influenceRect, property.FindPropertyRelative("influence"));
                }

                switch (propType.enumValueIndex)
                {
                    case 0: //color
                        EditorGUI.PropertyField(valueRect, property.FindPropertyRelative("colorValue"));
                        break;
                    case 1: //float
                        EditorGUI.PropertyField(valueRect, property.FindPropertyRelative("floatValue"));
                        break;
                    case 2: //int
                        EditorGUI.PropertyField(valueRect, property.FindPropertyRelative("intValue"));
                        break;
                    case 3: //matrix
                        EditorGUI.PropertyField(valueRect, property.FindPropertyRelative("matrixValue"));
                        break;
                    case 4: //texture
                        EditorGUI.PropertyField(valueRect, property.FindPropertyRelative("textureValue"));
                        break;
                    case 5: //vector
                        EditorGUI.PropertyField(valueRect, property.FindPropertyRelative("vectorValue"), true);
                        break;
                    default:
                        EditorGUI.LabelField(valueRect, "unsupported type?");
                        break;
                }
            }
            EditorGUI.EndProperty();

        }

    }

}