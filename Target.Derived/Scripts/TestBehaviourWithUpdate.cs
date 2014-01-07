using System;
using System.Collections.Generic;
using UnityEngine;

namespace Target.Derived
{
	public class TestBehaviourWithUpdate : Base.BaseMonoBehaviour
	{
		protected override void Start()
		{
			base.Start();

			Debug.Log("DerivedMonoBehaviour.Start");
		}

		public int count;
		protected override void Update()
		{
			base.Update();

			count++;
		}

		protected override void OnGUI()
		{
			GUILayout.Label("before " + count);

			base.OnGUI();

			GUILayout.Label("after " + count);
		}
	}
}
