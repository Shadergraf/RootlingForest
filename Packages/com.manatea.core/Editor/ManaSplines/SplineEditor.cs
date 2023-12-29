using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Manatea.SplineTool
{
    [CustomEditor(typeof(ManaSpline))]
    public class SplineEditor : Editor
    {

        private int lastHotID = 0;

        private ManaSpline Target {get {return (ManaSpline) this.target; }}

        void OnSceneGUI()
        {
            foreach(var seg in Target.splineData.Segments)
            {
                Handles.DrawAAPolyLine(seg.ArcPoints);

                foreach(var p in seg.ArcPoints)
                {
                    Handles.DrawSolidDisc(p, Vector3.forward, 0.05F);
                }

            }

            foreach (var point in Target.splineData.Points)
            {

                DrawSplinePointGizmo(point);

                lastHotID = EditorGUIUtility.hotControl;
            }
        }

        //TODO Clean Up
        private void DrawSplinePointGizmo(SplinePoint point) 
        {
            var handleColor = Handles.color;
            Handles.color = Color.white;

            var worldPoint  = Target.transform.TransformPoint(point.Position);
            var fmh_45_91_637916916199043249 = Quaternion.identity; var postDrag    = Handles.FreeMoveHandle(point.GetHashCode() + 0, worldPoint, HandleUtility.GetHandleSize(worldPoint) * 0.1F, Vector3.zero, Handles.SphereHandleCap);

            if (Vector3.Distance(Target.transform.TransformPoint(point.Position), postDrag) > 0.01F)
            {
                Undo.RecordObject(point, "Move Cap");
                point.Position = Target.transform.InverseTransformPoint(postDrag);
            }

            DrawSplineHandleGizmo(point, 0);

            DrawSplineHandleGizmo(point, 1);

            Handles.color = handleColor;
        }

        private void DrawSplineHandleGizmo(SplinePoint point, int tangent) 
        {
            var handleColor = Handles.color;
            Handles.color = Color.yellow;

            var handle = (tangent == 0) ? point.InHandlePosition : point.OutHandlePosition;

            var worldPoint = Target.transform.TransformPoint(handle + point.Position);

            Handles.DrawLine(worldPoint, Target.transform.TransformPoint(point.Position));

            var fmh_71_98_637916916199091203 = Quaternion.identity; var postDrag = Handles.FreeMoveHandle(point.GetHashCode() + tangent + 1, worldPoint, HandleUtility.GetHandleSize(worldPoint) * 0.1F, Vector3.zero, Handles.SphereHandleCap);

            if (Vector3.Distance(Target.transform.TransformPoint(handle + point.Position), postDrag) > 0.01F)
            {
                Undo.RecordObject(point, "Move Cap");

                if(tangent == 0) 
                {
                    point.InHandlePosition = Target.transform.InverseTransformPoint(postDrag - point.Position);
                }
                else 
                {
                    point.OutHandlePosition = Target.transform.InverseTransformPoint(postDrag - point.Position);
                }

            }

            Handles.color = handleColor;
        }


    }

}
