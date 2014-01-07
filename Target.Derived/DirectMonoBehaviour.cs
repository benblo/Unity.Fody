using System;
using System.Collections.Generic;

namespace Target.Derived
{
	public class DirectMonoBehaviour : UnityEngine.MonoBehaviour
	{
		int count;
		protected void OnGUI()
		{
			UnityEngine.GUILayout.Label("count " + count);
			count++;
		}
	}
}
