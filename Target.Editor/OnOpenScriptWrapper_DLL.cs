using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

public static class OnOpenScriptWrapper_DLL
{
	[OnOpenAsset]
	public static bool OnOpenAsset( int _instanceId, int _line )
	{
		Debug.Log(string.Format("OnOpenScriptWrapper_DLL {0}, {1}", _instanceId, _line));

		//string path = AssetDatabase.GetAssetPath(_instanceId);
		//var asset = EditorUtility.InstanceIDToObject(_instanceId);

		return false;
	}
}
