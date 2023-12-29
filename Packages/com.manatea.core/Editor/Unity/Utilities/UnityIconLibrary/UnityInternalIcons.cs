using UnityEngine;
using System;
using System.Collections.Generic;

namespace UnityEditor
{
	internal struct BuiltinIcon : IEquatable<BuiltinIcon>, IComparable<BuiltinIcon>
	{
		public GUIContent icon;
		public GUIContent name;

		public override bool Equals(object o) => o is BuiltinIcon && Equals((BuiltinIcon)o);
		public override int GetHashCode() => name.GetHashCode();
		public bool Equals(BuiltinIcon o) => name.text == o.name.text;
		public int CompareTo(BuiltinIcon o) => name.text.CompareTo(o.name.text);
	}

	public class UnityInternalIcons : EditorWindow
	{
		private List<BuiltinIcon> m_Icons = new List<BuiltinIcon>();
		private Vector2 m_ScrollPos;
		private GUIContent m_RefreshButton;


		[MenuItem("Window/UI Toolkit/Unity Internal Icons")]
		public static void ShowWindow()
		{
			UnityInternalIcons w = EditorWindow.GetWindow<UnityInternalIcons>();
			w.titleContent = new GUIContent("Unity Internal Icons");
		}


		private void OnEnable()
		{
			m_RefreshButton = new GUIContent(EditorGUIUtility.IconContent("d_preAudioLoopOff").image,
				"Refresh : Icons are only loaded in memory when the appropriate window is opened.");

			FindIcons();
		}

		private void OnGUI()
		{
			m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			if (GUILayout.Button(m_RefreshButton, EditorStyles.toolbarButton))
			{
				FindIcons();
			}
			GUILayout.FlexibleSpace();
			EditorGUILayout.LabelField("Found " + m_Icons.Count + " icons");
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.LabelField("Double-click name to copy", EditorStyles.centeredGreyMiniLabel);

			EditorGUILayout.Space();

			EditorGUIUtility.labelWidth = 100;
			for (int i = 0; i < m_Icons.Count; ++i)
			{
				EditorGUILayout.LabelField(m_Icons[i].icon, m_Icons[i].name);

				if (GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseDown && Event.current.clickCount > 1)
				{
					EditorGUIUtility.systemCopyBuffer = m_Icons[i].name.text;
					Debug.Log(m_Icons[i].name.text + " copied to clipboard.");
				}
			}

			EditorGUILayout.EndScrollView();
		}


		/* Find all textures and filter them to narrow the search. */
		private void FindIcons()
		{
			m_Icons.Clear();

			Texture2D[] t = Resources.FindObjectsOfTypeAll<Texture2D>();
			foreach (Texture2D x in t)
			{
				if (x.name.Length == 0)
					continue;

				if (x.hideFlags != HideFlags.HideAndDontSave && x.hideFlags != (HideFlags.HideInInspector | HideFlags.HideAndDontSave))
					continue;

				if (!EditorUtility.IsPersistent(x))
					continue;

				/* This is the *only* way I have found to confirm the icons are indeed unity builtin. Unfortunately
				 * it uses LogError instead of LogWarning or throwing an Exception I can catch. So make it shut up. */
				Debug.unityLogger.logEnabled = false;
				GUIContent gc = EditorGUIUtility.IconContent(x.name);
				Debug.unityLogger.logEnabled = true;

				if (gc == null)
					continue;
				if (gc.image == null)
					continue;

				m_Icons.Add(new BuiltinIcon()
				{
					icon = gc,
					name = new GUIContent(x.name)
				});
			}

			m_Icons.Sort();
			Resources.UnloadUnusedAssets();
			GC.Collect();
			Repaint();
		}
	}
}
