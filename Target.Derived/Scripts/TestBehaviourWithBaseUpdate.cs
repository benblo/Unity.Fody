using System;
using System.Collections.Generic;

namespace Target.Derived
{
	public class TestBehaviourWithBaseUpdate : Base.BaseMonoBehaviour
	{
		protected override void Update()
		{
			base.Update();
		}
	}
}
