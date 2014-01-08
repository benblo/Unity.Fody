using Reflected;
using SyntaxTree.VisualStudio.Unity.Bridge;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// What The Hack??
/// Unity doesn't call OnOpenAsset if the log comes from a DLL (file is outside the project --> no instanceID)
/// - so we detect a double-click over the console...
/// - get the selected log entry
/// - open it
/// 
/// BUT! if the log did come from a reachable script
/// - let it open normally
/// - mark the frame
/// - ignore the double-click (which seems to always happens at frame + 1)
/// </summary>
[InitializeOnLoad]
public static class OnOpenScriptWrapper_direct
{
	static int frameCount;
	static int lastOpenFrame;

	[OnOpenAsset]
	static bool OnOpenAsset( int _instanceId, int _line )
	{
		lastOpenFrame = frameCount;

		//Debug.Log(frameCount + "\t" + string.Format("OnOpenScriptWrapper_direct {0}, {1}", _instanceId, _line));

		//string path = AssetDatabase.GetAssetPath(_instanceId);
		//var asset = EditorUtility.InstanceIDToObject(_instanceId);

		return false;
	}

	static OnOpenScriptWrapper_direct()
	{
		EditorApplication.update += editorUpdate;
	}
	static void editorUpdate()
	{
		frameCount++;

		if (frameCount <= lastOpenFrame + 1)
		{
			return;
		}

		Event @event = EventExt.masterEvent;
		if (@event.type == EventType.Used &&
		    @event.clickCount == 2 &&
		    EditorWindow.focusedWindow.GetType() == ConsoleWindow.typeConsoleWindow)
		{
			onDoubleClickConsole();
		}
	}
	static void onDoubleClickConsole()
	{
		int selectedEntry = ConsoleWindow.selectedEntry;
		//Debug.Log(frameCount + "\tdouble-clicked log entry " + selectedEntry);

		if (selectedEntry == -1)
		{
			return;
		}

		LogEntry entry = new LogEntry(selectedEntry);
		OpenAssetUtility.OpenAsset(entry.file, entry.line);
	}
}

public static class OpenAssetUtility
{
	public static bool OpenAsset( string file, int line )
	{
		string path = EditorPrefs.GetString("kScriptsDefaultApp");
		//if (!File.Exists(Project.AssetFullPath(file)) || !path.EndsWith("UnityVS.OpenFile.exe"))
		//{
		//	return false;
		//}

		ProcessStartInfo startInfo = new ProcessStartInfo();
		startInfo.UseShellExecute = false;
		startInfo.CreateNoWindow = true;
		startInfo.Arguments = Project.QuotePathIfNeeded(Project.AssetFullPath(file)) + " " + ((line - 1)).ToString(CultureInfo.InvariantCulture);
		//startInfo.FileName = WinPath.Normalize(path);
		startInfo.FileName = path.Replace('/', '\\');
		Process.Start(startInfo);

		return true;
	}
}

namespace Reflected
{
	public static class LogEntries
	{
		static readonly Type r_type = ReflectionUtility.GetType(UnityNS.EditorInternal, "LogEntries");

		//public static extern int StartGettingEntries();
		public static int StartGettingEntries()
		{
			return (int)ReflectionUtility.InvokeStaticMethod(r_type, "StartGettingEntries");
		}

		//public static extern bool GetEntryInternal( int row, LogEntry outputEntry );
		public static bool GetEntryInternal( int row, object outputEntry )
		{
			return (bool)ReflectionUtility.InvokeStaticMethod(r_type, "GetEntryInternal", row, outputEntry);
		}

		//public static extern void EndGettingEntries();
		public static void EndGettingEntries()
		{
			ReflectionUtility.InvokeStaticMethod(r_type, "EndGettingEntries");
		}

		//public static extern int GetCount();
		//public static extern void GetCountsByType( ref int errorCount, ref int warningCount, ref int logCount );
		//public static extern int GetEntryCount( int row );
	}

	public class LogEntry
	{
		public LogEntry( int _row )
		{
			LogEntries.StartGettingEntries();

			entry = Activator.CreateInstance(r_type);
			LogEntries.GetEntryInternal(_row, entry);

			LogEntries.EndGettingEntries();
		}
		readonly object entry;

		static readonly Type r_type = ReflectionUtility.GetType(UnityNS.EditorInternal, "LogEntry");

		static readonly ReflectedField<string> r_condition = ReflectionUtility.GetFieldInfo(r_type, "condition");

		public string condition
		{
			get { return r_condition.getValue(entry); }
		}

		static readonly ReflectedField<int> r_errorNum = ReflectionUtility.GetFieldInfo(r_type, "errorNum");

		public int errorNum
		{
			get { return r_errorNum.getValue(entry); }
		}

		static readonly ReflectedField<string> r_file = ReflectionUtility.GetFieldInfo(r_type, "file");

		public string file
		{
			get { return r_file.getValue(entry); }
		}

		static readonly ReflectedField<int> r_line = ReflectionUtility.GetFieldInfo(r_type, "line");

		public int line
		{
			get { return r_line.getValue(entry); }
		}

		static readonly ReflectedField<int> r_mode = ReflectionUtility.GetFieldInfo(r_type, "mode");

		public int mode
		{
			get { return r_mode.getValue(entry); }
		}

		static readonly ReflectedField<int> r_instanceID = ReflectionUtility.GetFieldInfo(r_type, "instanceID");

		public int instanceID
		{
			get { return r_instanceID.getValue(entry); }
		}

		static readonly ReflectedField<int> r_identifier = ReflectionUtility.GetFieldInfo(r_type, "identifier");

		public int identifier
		{
			get { return r_identifier.getValue(entry); }
		}

		static readonly ReflectedField<int> r_isWorldPlaying = ReflectionUtility.GetFieldInfo(r_type, "isWorldPlaying");

		public int isWorldPlaying
		{
			get { return r_isWorldPlaying.getValue(entry); }
		}
	}

	public static class ConsoleWindow
	{
		public static readonly Type typeConsoleWindow = ReflectionUtility.GetType(UnityNS.Editor, "ConsoleWindow");

		//private static ConsoleWindow ms_ConsoleWindow;
		static readonly FieldInfo r_ms_ConsoleWindow = ReflectionUtility.GetFieldInfo(typeConsoleWindow, "ms_ConsoleWindow");

		static object ms_ConsoleWindow
		{
			get { return r_ms_ConsoleWindow.GetValue(null); }
		}

		//private ListViewState m_ListView;
		static readonly FieldInfo r_m_ListView = ReflectionUtility.GetFieldInfo(typeConsoleWindow, "m_ListView");

		static object m_ListView
		{
			get { return r_m_ListView.GetValue(ms_ConsoleWindow); }
		}

		// m_ListView.row
		static readonly Type typeListViewState = ReflectionUtility.GetType(UnityNS.Editor, "ListViewState");
		static readonly ReflectedField<int> r_row = ReflectionUtility.GetFieldInfo(typeListViewState, "row");

		public static int selectedEntry
		{
			get { return r_row.getValue(m_ListView); }
		}
	}
}
