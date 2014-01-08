using System;
using System.Collections.Generic;
using Reflected;
using UnityEditor;
using UnityEngine;

public class TestWindow : EditorWindow
{
	const string k_title = "Test";
	[MenuItem( "Window/Code/" + k_title )]
	static void CreateWindow()
	{
		var window = GetWindow<TestWindow>();
		window.title = k_title;
	}

	protected virtual void OnEnable()
	{
		autoRepaintOnSceneChange = true;
		Repaint();
	}
	protected virtual void OnSelectionChange()
	{
		Repaint();
	}
	protected virtual void OnHierarchyChange()
	{
		Repaint();
	}
	protected virtual void OnProjectChange()
	{
		Repaint();
	}
	//// Called 100 times per second on all visible windows.
	//protected virtual void Update() { }
	// Called at 10 frames per second to give the inspector a chance to update
	protected virtual void OnInspectorUpdate()
	{
		Repaint();
	}

	protected virtual void OnGUI()
	{
		int selectedEntry = ConsoleWindow.selectedEntry;
		if (selectedEntry == -1)
		{
			return;
		}

		LogEntry entry = new LogEntry(selectedEntry);
		GUILayout.Label(entry.condition);
		GUILayout.Label(entry.file);
		GUILayout.Label(entry.line.ToString());

		GUI.enabled = entry.file.EndsWith(".cs");
		{
			if (GUILayout.Button("open"))
			{
				OpenAssetUtility.OpenAsset(entry.file, entry.line);
			}
		}
		GUI.enabled = true;
	}
}
