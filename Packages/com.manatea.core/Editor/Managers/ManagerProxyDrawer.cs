using UnityEditor;
using UnityEngine;

namespace Manatea.Managers
{
    [CustomPropertyDrawer(typeof(ManagerProxy), true)]
    public class ManagerProxyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!fieldInfo.FieldType.IsSubclassOf(typeof(ScriptableObject))
                || property.hasMultipleDifferentValues
                || property.objectReferenceValue)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            Rect createButtonRect = position;
            createButtonRect.xMin = createButtonRect.xMax - 60;
            position.xMax -= createButtonRect.width;

            EditorGUI.PropertyField(position, property, label);

            if (GUI.Button(createButtonRect, "Create", EditorStyles.miniButton))
                EditorApplication.delayCall += () => CreateScriptable(property);
        }

        private void CreateScriptable(SerializedProperty property)
        {
            string filePath = EditorUtility.SaveFilePanelInProject("Create " + fieldInfo.FieldType.Name, fieldInfo.FieldType.Name, "asset", "blub");

            if (!string.IsNullOrEmpty(filePath))
            {
                ScriptableObject scriptableObject = ScriptableObject.CreateInstance(fieldInfo.FieldType);
                AssetDatabase.CreateAsset(scriptableObject, filePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                property.objectReferenceValue = scriptableObject;
                property.serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
