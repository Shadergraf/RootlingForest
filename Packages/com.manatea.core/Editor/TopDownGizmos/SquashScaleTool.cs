using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.ShortcutManagement;

namespace Manatea.TopDownGizmos
{
    [EditorTool("Squash Scale Tool", typeof(Transform))]
    public class SquashScaleTool : EditorTool
    {

        [SerializeField]
        Texture2D icon;

        private GUIContent content;

        public override GUIContent toolbarIcon
        {
            get 
            {
                if(content == null)
                {
                    content = new GUIContent(icon);
                }

                return content;
            }
        }


        [Shortcut("SquashScale", null, KeyCode.R, ShortcutModifiers.Shift)]
        public static void SelectTool()
        {
            ToolManager.SetActiveTool<SquashScaleTool>();
        }



        public override void OnToolGUI(EditorWindow window)
        {
            if(window.GetType() != typeof(SceneView))
            {
                return;
            }

            base.OnToolGUI(window);


            Vector3 position = Tools.handlePosition;

            var size = HandleUtility.GetHandleSize(position) * 0.5F;

            foreach (var transform in Selection.transforms)
            {

                Vector3 scale = transform.localScale;

                EditorGUI.BeginChangeCheck();
                {

                    scale = Handles.ScaleHandle(scale, position, Quaternion.Euler(0,180,0), size * 2);
    
                }
                if (EditorGUI.EndChangeCheck())
                {
                    Vector3 delta = scale - transform.localScale;

                    if((delta.magnitude - Mathf.Abs(delta.x) > 0.01F) && (delta.magnitude - Mathf.Abs(delta.y) > 0.01F) && (delta.magnitude - Mathf.Abs(delta.z) > 0.01F))
                    {
                        Undo.RecordObjects(Selection.transforms, "Scale");
                        transform.localScale += delta;
                    }
                    else
                    {
                        Undo.RecordObjects(Selection.transforms, "Squash Scale");
                        transform.localScale = CalculateScale(transform.localScale, delta);
                    }

                }
            }

        }


        private Vector3 CalculateScale(Vector3 ogScale, Vector3 delta)
        {
            if (delta.sqrMagnitude < 0.001F)
                return ogScale;

            var volume = ogScale.x * ogScale.y * ogScale.z;

            Vector3 offset = ogScale;

            if(Mathf.Abs(Vector3.Dot(Vector3.right, delta.normalized)) > 0.97F )
            {
                // x transformed -> informs y and z scale
                offset.x = ogScale.x + delta.x;

                offset.y = volume / (offset.x * ogScale.z);
                offset.z = volume / (offset.x * ogScale.y);
            }
            else if (Mathf.Abs(Vector3.Dot(Vector3.up, delta.normalized)) > 0.97F)
            {
                // y transformed -> informs x and z scale
                offset.y = ogScale.y + delta.y;

                offset.x = volume / (offset.y * ogScale.z);
                offset.z = volume / (offset.y * ogScale.x);

            }
            else if(Mathf.Abs(Vector3.Dot(Vector3.forward, delta.normalized)) > 0.97F)
            {
                // z transformed -> informs x and y scale
                offset.z = ogScale.z + delta.z;

                offset.x = volume / (offset.z * ogScale.y);
                offset.y = volume / (offset.z * ogScale.x);
            }

            return offset;
        }


    }

}