using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.ShortcutManagement;

namespace Manatea.TopDownGizmos
{
    [EditorTool("TopDown Move Tool", typeof(Transform))]
    public class TopDownMoveGizmo : EditorTool
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

        [Shortcut("TopDown Move", null, KeyCode.W, ShortcutModifiers.Shift)]
        public static void SelectTool()
        {
            ToolManager.SetActiveTool<TopDownMoveGizmo>();
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

            EditorGUI.BeginChangeCheck();
            {
                using (new Handles.DrawingScope(Color.green))
                {
                    position = Handles.Slider(position, Vector3.up);
                }

                using (new Handles.DrawingScope(new Color(1, 0.92F, 0.016F, 0.25F)))
                {
                    Handles.DrawSolidDisc(position, Vector3.up, size);
                }

                using (new Handles.DrawingScope(Color.yellow))
                {
                    Handles.DrawWireDisc(position, Vector3.up, size, 3);

                    position = Handles.Slider2D(position, Vector3.up, Vector3.forward, Vector3.right, size, Handles.CircleHandleCap, 0);
                }

                using (new Handles.DrawingScope(Color.red))
                {
                    DoRaycasts();
                }

            }
            if (EditorGUI.EndChangeCheck())
            {
                Vector3 delta = position - Tools.handlePosition;

                Undo.RecordObjects(Selection.transforms, "Move Platform");

                if (Event.current.modifiers == EventModifiers.Shift)
                {
                    SceneView.lastActiveSceneView.pivot += delta / 5;

                    foreach (var transform in Selection.transforms)
                        transform.position += delta / 5;                //move linear

                }
                else
                {
                    foreach (var transform in Selection.transforms)
                        transform.position += delta;                //move linear
                }

            }
        }

        private void DoRaycasts()
        {
            Vector3 position = Tools.handlePosition;

            var hits = Physics.RaycastAll(position, Vector3.down, 10F);

            var furthest = position;

            foreach (var hit in hits)
            {
                Handles.DrawWireDisc(hit.point, Vector3.up, HandleUtility.GetHandleSize(position) * 0.1F);

                if(Vector3.Distance(position, hit.point) > Vector3.Distance(position, furthest))
                {
                    furthest = hit.point; //measure the hit points for distance to get the furthest
                }
            }

            Handles.DrawDottedLine(position, furthest, 1);

        }

    }

}