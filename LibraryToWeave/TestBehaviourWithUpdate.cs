using System;
using System.Collections.Generic;

namespace LibraryToWeave
{
	public class TestBehaviourWithUpdate : BaseMonoBehaviour
	{
		protected override void Update()
		{
			base.Update();

			System.Diagnostics.Debug.Print("hello");
			System.Diagnostics.Debug.Print("world");
		}
	}
}
