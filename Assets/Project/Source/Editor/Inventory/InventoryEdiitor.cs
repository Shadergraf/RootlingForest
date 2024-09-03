using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using Manatea;

[CustomEditor(typeof(Inventory))]
public class InventoryEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (EditorApplication.isPlaying)
        {
            EditorGUILayout.LabelField("Inventory contents:");
            EditorGUI.indentLevel++;
            Inventory inventory = (Inventory)target;
            for (int i = 0; i < inventory.Size; i++)
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(string.Format("Item {0}:", i));

                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField(inventory.GetItemAt(i), typeof(GameObject), true);
                EditorGUI.EndDisabledGroup();

                if (GUILayout.Button("▲"))
                {
                    inventory.SwapItems(i, MMath.Mod(i - 1, inventory.Size));
                }
                if (GUILayout.Button("▼"))
                {
                    inventory.SwapItems(i, MMath.Mod(i + 1, inventory.Size));
                }
                if (GUILayout.Button("Remove"))
                {
                    inventory.RemoveItem(i);
                }

                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
        }
    }
}
