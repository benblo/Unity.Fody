using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibraryToWeave
{
	public class TestBehaviourWithUpdate : MonoBehaviour
	{
		protected override void Update()
		{
			base.Update();

			System.Diagnostics.Debug.Print("hello");
			System.Diagnostics.Debug.Print("world");
		}
	}
}
