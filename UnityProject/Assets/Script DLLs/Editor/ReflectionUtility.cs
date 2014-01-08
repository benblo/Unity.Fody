#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

using System.Reflection;

public enum UnityNS { Engine, Editor, EditorInternal }	// shortcut for GetType stuff

public static class ReflectionUtility
{
	public static Type GetType(UnityNS assembly, string typeName)
	{
		// Qualified name syntax: "Namespace.Type,Assembly"
		switch (assembly)
		{
			case UnityNS.Engine:
				return GetType("UnityEngine." + typeName + ",UnityEngine");
			case UnityNS.Editor:
				return GetType("UnityEditor." + typeName + ",UnityEditor");
			case UnityNS.EditorInternal:
				return GetType("UnityEditorInternal." + typeName + ",UnityEditor");
			default:
				throw new ArgumentOutOfRangeException("assembly");
		}

		//return GetType("Unity" + assembly + "." + typeName + ",Unity" + assembly);	// doesn't work for ns UnityEditorInternal, contained in assembly UnityEditor
	}
	public static Type GetType(string qualifiedTypeName)
	{
		Type type = Type.GetType(qualifiedTypeName);
		if (type != null)
			return type;

		throw new System.Exception("Type not found! " + qualifiedTypeName);
	}

	public const BindingFlags staticFlags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
	public const BindingFlags instanceFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
	public const BindingFlags anyFlags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;


	public static object InvokeStaticMethod(UnityNS assembly, string typeName, string methodName, params object[] parameters)
	{
		Type type = GetType(assembly, typeName);
		return InvokeStaticMethod(type, methodName, parameters);
	}
	public static object InvokeStaticMethod(Type type, string methodName, params object[] parameters)
	{
		Type[] types = Array.ConvertAll(parameters, p => p.GetType());
		MethodInfo method = type.GetMethod(methodName, staticFlags, null, types, null);
		return method.Invoke(null, parameters);
	}
	public static object InvokeMethod(object obj, string methodName, params object[] parameters)
	{
		Type type = obj.GetType();
		MethodInfo method = type.GetMethod(methodName, instanceFlags);
		return method.Invoke(obj, parameters);
	}

	public static MethodInfo GetMethodInfo(UnityNS assembly, string typeName, string methodName)
	{
		Type type = GetType(assembly, typeName);
		return GetMethodInfo(type, methodName);
	}
	public static MethodInfo GetMethodInfo(Type type, string methodName)
	{
		return type.GetMethod(methodName, anyFlags);
	}

	public static TDelegate GetStaticDelegate<TDelegate>( UnityNS assembly, string typeName, string methodName )
	{
		Type type = GetType(assembly, typeName);
		return GetStaticDelegate<TDelegate>(type, methodName);
	}
	public static TDelegate GetStaticDelegate<TDelegate>( Type type, string methodName )
	{
		var method = type.GetMethod(methodName, anyFlags);
		return (TDelegate)(object)Delegate.CreateDelegate(typeof(TDelegate), method);
	}


	public static object GetStaticField(UnityNS assembly, string typeName, string fieldName)
	{
		Type type = GetType(assembly, typeName);
		return GetStaticField(type, fieldName);
	}
	public static object GetStaticField(Type type, string fieldName)
	{
		FieldInfo field = type.GetField(fieldName, staticFlags);
		return field.GetValue(null);
	}
	public static object GetField(object obj, string fieldName)
	{
		Type type = obj.GetType();
		FieldInfo field = type.GetField(fieldName, instanceFlags);
		return field.GetValue(obj);
	}

	public static void SetStaticField(UnityNS assembly, string typeName, string fieldName, object value)
	{
		Type type = GetType(assembly, typeName);
		SetStaticField(type, fieldName, value);
	}
	public static void SetStaticField(Type type, string fieldName, object value)
	{
		FieldInfo field = type.GetField(fieldName, staticFlags);
		field.SetValue(null, value);
	}
	public static void SetField(object obj, string fieldName, object value)
	{
		Type type = obj.GetType();
		FieldInfo field = type.GetField(fieldName, instanceFlags);
		field.SetValue(obj, value);
	}

	public static FieldInfo GetStaticFieldInfo(UnityNS assembly, string typeName, string fieldName)
	{
		Type type = GetType(assembly, typeName);
		return GetStaticFieldInfo(type, fieldName);
	}
	public static FieldInfo GetStaticFieldInfo(Type type, string fieldName)
	{
		return type.GetField(fieldName, staticFlags);
	}
	public static FieldInfo GetFieldInfo(UnityNS assembly, string typeName, string fieldName)
	{
		Type type = GetType(assembly, typeName);
		return GetFieldInfo(type, fieldName);
	}
	public static FieldInfo GetFieldInfo(Type type, string fieldName)
	{
		return type.GetField(fieldName, anyFlags);
	}


	public static PropertyInfo GetStaticPropertyInfo(UnityNS assembly, string typeName, string propertyName)
	{
		Type type = GetType(assembly, typeName);
		return GetStaticPropertyInfo(type, propertyName);
	}
	public static PropertyInfo GetStaticPropertyInfo(Type type, string propertyName)
	{
		return type.GetProperty(propertyName, staticFlags);
	}
	public static PropertyInfo GetPropertyInfo(object obj, string propertyName)
	{
		Type type = obj.GetType();
		return type.GetProperty(propertyName, instanceFlags);
	}

	public static object GetStaticProperty(UnityNS assembly, string typeName, string propertyName)
	{
		Type type = GetType(assembly, typeName);
		return GetStaticProperty(type, propertyName);
	}
	public static object GetStaticProperty(Type type, string propertyName)
	{
		PropertyInfo property = type.GetProperty(propertyName, staticFlags);
		return property.GetGetMethod().Invoke(null, null);
	}
	public static object GetProperty(object obj, string propertyName)
	{
		Type type = obj.GetType();
		PropertyInfo property = type.GetProperty(propertyName, instanceFlags);
		return property.GetGetMethod().Invoke(obj, null);
	}

	public static object SetStaticProperty(UnityNS assembly, string typeName, string propertyName)
	{
		Type type = GetType(assembly, typeName);
		return SetStaticProperty(type, propertyName);
	}
	public static object SetStaticProperty(Type type, string propertyName)
	{
		PropertyInfo property = type.GetProperty(propertyName, staticFlags);
		return property.GetSetMethod().Invoke(null, null);
	}
	public static object SetProperty(object obj, string propertyName)
	{
		Type type = obj.GetType();
		PropertyInfo property = type.GetProperty(propertyName, instanceFlags);
		return property.GetSetMethod().Invoke(obj, null);
	}

	public static PropertyInfo GetPropertyInfo(UnityNS assembly, string typeName, string propertyName)
	{
		Type type = GetType(assembly, typeName);
		return GetPropertyInfo(type, propertyName);
	}
	public static PropertyInfo GetPropertyInfo(Type type, string propertyName)
	{
		return type.GetProperty(propertyName, anyFlags);
	}


	public static Type[] GetAllEngineTypes()
	{
		Assembly engineAssembly = Assembly.GetAssembly(typeof(Object));
		return engineAssembly.GetExportedTypes();
	}
	public static Type[] FindEngineClassesSubclassOf(Type parentType)
	{
		return Array.FindAll(GetAllEngineTypes(), t => t.IsSubclassOf(parentType));
	}
	/// <summary>
	/// WARNING: can only be used in-editor!
	/// </summary>
	public static Type[] FindEditorClassesSubclassOf(Type parentType)
	{
		ArrayList list = (ArrayList)ReflectionUtility.InvokeStaticMethod(UnityNS.Editor, "AttributeHelper", "FindEditorClassesSubclassOf", parentType);
		return (Type[])list.ToArray(typeof(Type));
	}
}

public class ReflectedProperty<T>
{
	public ReflectedProperty(PropertyInfo _prop)
	{
		m_prop = _prop;
	}

	public static implicit operator ReflectedProperty<T>(PropertyInfo _prop)
	{
		return new ReflectedProperty<T>(_prop);
	}


	PropertyInfo m_prop;
	public T StaticValue
	{
		get { return (T)m_prop.GetValue(null, null); }
		set { m_prop.SetValue(null, value, null); }
	}
}

public class ReflectedField<T>
{
	public ReflectedField(FieldInfo field)
	{
		m_field = field;
	}

	public static implicit operator ReflectedField<T>(FieldInfo _prop)
	{
		return new ReflectedField<T>(_prop);
	}


	readonly FieldInfo m_field;
	public T StaticValue
	{
		get { return (T)m_field.GetValue(null); }
		set { m_field.SetValue(null, value); }
	}
	public T getValue(object _obj)
	{
		return (T)m_field.GetValue(_obj);
	}
	public void setValue(object _obj, T _value)
	{
		m_field.SetValue(_obj, _value);
	}
}

#endif