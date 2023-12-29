using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;

namespace Manatea.SplineTool
{
    public class SplineAssetFactory
    {
        [MenuItem("Assets/Create/Manatea/Splines/Spline", false, -10000)]
        public static void CreateSplineAsset()
        {
            var instance = Spline.CreateSpline();

            string assetPath;
            var gotAssetPath = TryGetActiveFolderPath(out assetPath);

            if(!gotAssetPath)
            {
                Debug.LogWarning("Could not locate filePath!");
                return;
            }

            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath + "/Spline.asset");

            AssetDatabase.CreateAsset(instance, assetPath);

            foreach (var point in instance.Points)
            {
                point.hideFlags |= HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(point, assetPath);
            }

            foreach (var seg in instance.Segments)
            {
                seg.hideFlags |= HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(seg, assetPath);
            }

            AssetDatabase.SaveAssets();
            Selection.activeObject = instance;
        }

        // <summary> Try to find the folder currently focused by the current Project Window. </summary>
        private static bool TryGetActiveFolderPath( out string path )
        {
            var _tryGetActiveFolderPath = typeof(ProjectWindowUtil).GetMethod( "TryGetActiveFolderPath", BindingFlags.Static | BindingFlags.NonPublic );

            object[] args = new object[] { null };
            bool found = (bool)_tryGetActiveFolderPath.Invoke( null, args );
            path = (string)args[0];

            return found;
        }

    }
}