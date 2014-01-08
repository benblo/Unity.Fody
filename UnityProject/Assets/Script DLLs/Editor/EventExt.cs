#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEditor;

public static class EventExt
{
	static readonly ReflectedField<Event> masterEventField = ReflectionUtility.GetStaticFieldInfo(typeof(Event), "s_MasterEvent");

	/// <summary>
	/// Event.current always returns null outside of OnGUI functions.
	/// masterEvent will basically return the last known event.
	/// </summary>
	public static Event masterEvent
	{
		get
		{
			return masterEventField.StaticValue ?? new Event();	// for recompilation etc, return an empty event instead of a null one
		}
	}
	public static EventModifiers modifiers { get { return masterEvent.modifiers; } }
	public static bool shift { get { return masterEvent.shift; } }
	public static bool alt { get { return masterEvent.alt; } }
	public static bool control { get { return masterEvent.control; } }
}

#endif