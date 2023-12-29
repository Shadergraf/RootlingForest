using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Manatea.ModelShuffle
{

    [CustomEditor(typeof(ModelShuffler))]
    [CanEditMultipleObjects]
    public class ModelShufflerEditor : Editor
    {

        private ModelShuffler Target => (ModelShuffler)target;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();


            //EditorGUI.BeginDisabledGroup(Target.candidates != null && Target.candidates.Count > 0);

            //if (GUILayout.Button("Populate With Children"))
            //{
            //    foreach (var t in targets)
            //    {
            //        var T = (ModelShuffler)t;

            //        if (T.candidates == null || T.candidates.Count < 1)
            //        {
            //            foreach (Transform child in T.transform)
            //            {
            //                T.candidates.Add(new ModelShuffler.ShuffleCandidate(child.gameObject));
            //            }
            //        }
            //    }
            //}

            //EditorGUI.EndDisabledGroup();

            //EditorGUILayout.Separator();
            //EditorGUILayout.Separator();
            EditorGUILayout.Separator();

            if (GUILayout.Button("Pick Again", GUILayout.Height(EditorGUIUtility.singleLineHeight * 1.5F)))
            {
                foreach (var t in targets)
                {
                    ((ModelShuffler)t).Shuffle();
                }
            }
        }

    }

}