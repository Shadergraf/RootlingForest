using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Manatea.ModelShuffle
{
    [CustomPropertyDrawer(typeof(ModelShuffler.ShuffleCandidate))]
    public class CandidateDrawer : PropertyDrawer
    {

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return base.GetPropertyHeight(property, label);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            EditorGUI.indentLevel++;

            var rects = SplitHorizontal(position);

            EditorGUI.PropertyField(rects[0], property.FindPropertyRelative("obj"), GUIContent.none);

            EditorGUI.PropertyField(rects[1], property.FindPropertyRelative("relativeProbability"));

            EditorGUI.indentLevel--;

            EditorGUI.EndProperty();
        }

        private Rect[] SplitHorizontal(Rect rect)
        {
            var rects = new Rect[2];

            rects[0] = new Rect(rect);

            rects[0].position = new Vector2(rect.position.x / 2F, rect.position.y);
            rects[0].size = new Vector2(rect.size.x / 2F, rect.size.y);


            rects[1] = new Rect(rect);

            rects[1].position = new Vector2(rects[0].position.x + rects[0].size.x , rect.position.y);
            rects[1].size = new Vector2(rect.size.x / 2F, rect.size.y);


            return rects;
        }

    }

}