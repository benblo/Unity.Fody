using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Weavers
{
	public class ModuleWeaver
	{
		public Action<string> LogInfo { get; set; }
		public Action<string> LogWarning { get; set; }
		public Action<string, SequencePoint> LogWarningPoint { get; set; }
		public Action<string> LogError { get; set; }
		public Action<string, SequencePoint> LogErrorPoint { get; set; }

		// Init logging delegates to make testing easier
		public ModuleWeaver()
		{
			LogInfo = m => { };
			LogWarning = m => { };
			LogWarningPoint = ( m, p ) => { };
			LogError = m => { };
			LogErrorPoint = ( m, p ) => { };
		}

		public IAssemblyResolver AssemblyResolver { get; set; }
		public ModuleDefinition ModuleDefinition { get; set; }
		public List<string> DefineConstants { get; set; }
		public string AssemblyFilePath { get; set; }
		public string ProjectDirectoryPath { get; set; }
		public string AddinDirectoryPath { get; set; }
		public string SolutionDirectoryPath { get; set; }

		const string BaseMonoBehaviour = "BaseMonoBehaviour";
		const string BaseMonoBehaviourFull = "Target.Base.BaseMonoBehaviour";

		public void Execute()
		{
			//Console.WriteLine("EXECUTING FODY Console.WriteLine");
			//System.Diagnostics.Debug.WriteLine("EXECUTING FODY Debug.WriteLine");
			//System.Diagnostics.Trace.WriteLine("EXECUTING FODY Trace.WriteLine");
			//LogInfo("EXECUTING FODY LogInfo");
			//LogWarning("EXECUTING FODY LogWarning");
			//LogError("EXECUTING FODY LogError");
			//LogWarning(ModuleDefinition.Assembly.ToString());

			LogWarning("EXECUTING FODY ModuleDefinition : " + ModuleDefinition);

			switch (ModuleDefinition.Assembly.Name.Name)
			{
				case "Target.Base":
					execute_Base();
					break;
				case "Target.Derived":
					execute_Derived();
					break;
				case "LibraryToWeave":
					execute_LibraryToWeave();
					break;
			}
		}

		void execute_Base()
		{
			var baseClass = ModuleDefinition.Types[1];
			LogWarning("EXECUTING FODY baseClass : " + baseClass.Name);
			addLogMethod(baseClass, "fody_TargetBase");
			//removeUnityMethod(baseClass, "Update");
			//removeUnityMethod(baseClass, "OnGUI");

			removeReferenceToMscorlib40(ModuleDefinition);
		}

		void execute_Derived()
		{
			TypeReference baseTypeReference;
			if (!ModuleDefinition.TryGetTypeReference(BaseMonoBehaviourFull, out baseTypeReference))
			{
				LogWarning("ERROR: cannot find BaseMonoBehaviour");
				return;
			}

			// my module
			{
				removeUnityMethodReferences(ModuleDefinition, baseTypeReference);
				removeReferenceToMscorlib40(ModuleDefinition);

				typesToWrap.Clear();

				foreach (var type in ModuleDefinition.Types)
				{
					if (type.BaseType == baseTypeReference)
					{
						processDerivedMonoBehaviour(type);
						continue;
					}
				}

				generateWrappers(@"D:\Users\Benoit FOULETIER\Documents\GitHub\Unity.Fody\UnityProject\Assets\Base DLL\Wrappers\");
			}

			// base module
			TypeDefinition baseTypeDefinition = baseTypeReference.Resolve();
			ModuleDefinition baseModule = baseTypeDefinition.Module;
			{
				removeUnityMethods(baseTypeDefinition);
				removeReferenceToMscorlib40(baseModule);
			}

			baseModule.Assembly.Write(
				@"D:\Users\Benoit FOULETIER\Documents\GitHub\Unity.Fody\UnityProject\Assets\Base DLL\Target.Base.dll");
			ModuleDefinition.Assembly.Write(
				@"D:\Users\Benoit FOULETIER\Documents\GitHub\Unity.Fody\UnityProject\Assets\Base DLL\Target.Derived.dll");
		}
		void removeUnityMethodReferences( ModuleDefinition _module, TypeReference _baseType )
		{
			var memberReferences = _module.GetMemberReferences();
			foreach (var memberReference in memberReferences)
			{
				if (memberReference.DeclaringType != _baseType)
				{
					continue;
				}

				MethodReference baseMethod = memberReference as MethodReference;
				if (baseMethod == null)
				{
					continue;
				}

				if (isUnityMethod(baseMethod.Name))
				{
					baseMethod.Name = "dummy_" + baseMethod.Name;
				}
			}
		}
		void removeUnityMethods( TypeDefinition _baseType )
		{
			foreach (var baseMethod in _baseType.Methods)
			{
				if (isUnityMethod(baseMethod.Name))
				{
					baseMethod.Name = "dummy_" + baseMethod.Name;
				}
			}
		}

		void execute_LibraryToWeave()
		{
			typesToWrap.Clear();

			foreach (var type in ModuleDefinition.Types)
			{
				if (type.Name == BaseMonoBehaviour)
				{
					processBaseMonoBehaviour(type);
					continue;
				}

				if (type.BaseType != null &&
				    type.BaseType.Name == BaseMonoBehaviour)
				{
					processDerivedMonoBehaviour(type);
					continue;
				}
			}

			generateWrappers(@"D:\Users\Benoit FOULETIER\Documents\GitHub\Unity.Fody\UnityProject\Assets\Script wrappers (autogen)");


			removeReferenceToMscorlib40(ModuleDefinition);
		}

		void processBaseMonoBehaviour( TypeDefinition _type )
		{
			foreach (var unityMethod in unityMethods)
			{
				removeUnityMethod(_type, unityMethod);
			}
		}
		void removeUnityMethod( TypeDefinition _type, string _methodName )
		{
			var update = _type.Methods.FirstOrDefault(m => m.Name == _methodName);
			if (update != null)
			{
				update.Name = "dummy_" + _methodName;
				//_type.Methods.Remove(update);
			}
		}
		static readonly string[] unityMethods = new[]
		{
			"Start",
			"Update",
			"OnGUI",
		};
		static bool isUnityMethod( string _name )
		{
			switch (_name)
			{
				case "Start":
				case "Update":
				case "OnGUI":
					return true;
			}

			return false;
		}

		readonly List<TypeDefinition> typesToWrap = new List<TypeDefinition>();
		void processDerivedMonoBehaviour( TypeDefinition _type )
		{
			if (!_type.IsAbstract)
			{
				typesToWrap.Add(_type);

				_type.IsAbstract = true;
			}
		}
		void generateWrappers(string _outputPath)
		{
			DirectoryInfo wrapperDirectory = new DirectoryInfo(_outputPath);
			var wrappers = new List<FileInfo>(wrapperDirectory.GetFiles("*.cs"));

			List<TypeDefinition> typesToAdd = new List<TypeDefinition>();

			foreach (var type in typesToWrap)
			{
				var wrapper = wrappers.Find(w => w.Name == type.Name + ".cs");

				if (wrapper != null)
				{
					wrappers.Remove(wrapper);
					continue;
				}

				typesToAdd.Add(type);
			}

			if (wrappers.Count == 1 &&
			    typesToAdd.Count == 1)
			{
				// consider it a rename
				var wrapper = wrappers[0];
				var type = typesToAdd[0];

				var oldMetaPath = wrapper.FullName + ".meta";
				if (File.Exists(oldMetaPath))
				{
					var newMetaPath = Path.Combine(wrapperDirectory.FullName, type.Name + ".cs.meta");
					File.Move(oldMetaPath, newMetaPath);
				}

				var newWrapperPath = Path.Combine(wrapperDirectory.FullName, type.Name + ".cs");
				wrapper.MoveTo(newWrapperPath);

				writeWrapper(wrapper.FullName, type);
				return;
			}

			if (wrappers.Count > 0)
			{
				// some types left to remove

				foreach (var wrapper in wrappers)
				{
					var meta = wrapper.FullName + ".meta";
					if (File.Exists(meta))
					{
						File.Delete(meta);
					}

					wrapper.Delete();
				}
			}

			foreach (var type in typesToAdd)
			{
				var wrapperPath = Path.Combine(wrapperDirectory.FullName, type.Name + ".cs");
				writeWrapper(wrapperPath, type);
			}
		}
		static void writeWrapper( string _wrapperPath, TypeDefinition _type )
		{
			var content = string.Format("public class {0} : {1}.{0} {{}}",
				_type.Name,
				_type.Namespace);

			File.WriteAllText(_wrapperPath, content);
		}

		static void addLogMethod( TypeDefinition _type, string _name )
		{
			TypeReference voidRef = _type.Module.Import(typeof(void));
			_type.Methods.Add(new MethodDefinition(_name, MethodAttributes.Public, voidRef));
		}
		static void removeReferenceToMscorlib40( ModuleDefinition _module )
		{
			// remove reference to mscorlib 4.0 (added by Fody?)
			var mscorlib4 = _module.AssemblyReferences.FirstOrDefault(a => a.Version.Major == 4 && a.Name == "mscorlib");
			if (mscorlib4 != null)
			{
				_module.AssemblyReferences.Remove(mscorlib4);
			}
		}
	}
}
