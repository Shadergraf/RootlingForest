using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(Matrix4x4))]
public class Matrix4x4Drawer : PropertyDrawer
{

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return base.GetPropertyHeight(property, label) * 6;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var cellSize = position.size / 6F;

        EditorGUI.BeginProperty(position, label, property);
        {

            var rect = new Rect(position.min + 0.5F * cellSize, cellSize);
            var lineStart = rect.position;

            for (int x = 0; x < 5; x++)
            {
                for (int y = 0; y < 5; y++)
                {

                    if (x == 0 && y == 0)
                    { 
                        //no label here
                    }
                    else if (x == 0 || y == 0)
                    {
                        var lineLabel = string.Format("m_{0}{1}", (x != 0) ? (x - 1).ToString() : "x", (y != 0) ? (y - 1).ToString() : "x");

                        EditorGUI.LabelField(rect, new GUIContent(lineLabel), EditorStyles.centeredGreyMiniLabel);
                    }
                    else
                    {
                        EditorGUI.PropertyField(rect, property.FindPropertyRelative(string.Format("e{0}{1}", x - 1, y - 1)), GUIContent.none);
                    }


                    rect.position += Vector2.up * cellSize;
                }
                rect.position = new Vector2(rect.position.x, lineStart.y);
                rect.position += Vector2.right * cellSize;
            }

        }
        EditorGUI.EndProperty();

    }

}
