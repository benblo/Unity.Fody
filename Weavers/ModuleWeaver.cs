using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Mdb;
using Mono.Cecil.Pdb;

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

		public ModuleDefinition ModuleDefinition { get; set; }
		public string AssemblyFilePath { get; set; }
		public string ProjectDirectoryPath { get; set; }
		public string AddinDirectoryPath { get; set; }
		public string SolutionDirectoryPath { get; set; }
		public IAssemblyResolver AssemblyResolver { get; set; }

		const string BaseMonoBehaviour = "BaseMonoBehaviour";
		const string BaseMonoBehaviourFull = "Target.Base.BaseMonoBehaviour";
		const string BaseModuleName = "Target.Base";

		const string outputPath = @"UnityProject\Assets\";
		const string outputDllPath = outputPath + @"Script DLLs\";
		const string outputWrapperPath = outputPath + @"Script wrappers (autogen)\";

		public void Execute()
		{
			if (ModuleDefinition.Name == BaseModuleName + ".dll")
			{
				LogInfo("patching base module " + ModuleDefinition);
				executeBase(ModuleDefinition);
			}
			else
			{
				LogInfo("patching derived module " + ModuleDefinition);
				executeDerived(ModuleDefinition);
			}
		}
		void executeBase( ModuleDefinition _module )
		{
			TypeDefinition baseTypeDefinition = _module.Types.First(t => t.Name == BaseMonoBehaviour);
			removeUnityMethods(baseTypeDefinition);
			removeReferenceToMscorlib40(_module);

			saveToUnityProject(_module);
		}
		void executeDerived( ModuleDefinition _module )
		{
			TypeReference baseTypeReference;
			if (!_module.TryGetTypeReference(BaseMonoBehaviourFull, out baseTypeReference))
			{
				LogError("ERROR: cannot find " + BaseMonoBehaviour);
				return;
			}

			removeUnityMethodReferences(_module, baseTypeReference);
			removeReferenceToMscorlib40(_module);

			typesToWrap.Clear();

			foreach (var type in _module.Types)
			{
				if (type.BaseType == baseTypeReference)
				{
					processDerivedMonoBehaviour(type);
					continue;
				}
			}

			generateWrappers();

			saveToUnityProject(_module);
		}
		void saveToUnityProject( ModuleDefinition _module )
		{
			// change the destination output, so the file is not changed locally
			InnerWeaver weaver = (InnerWeaver)AssemblyResolver;
			weaver.AssemblyFilePath = SolutionDirectoryPath + outputDllPath + _module;

			// TODO: pdb2mdb here?

			//// write to a custom path
			//InnerWeaver weaver = (InnerWeaver)AssemblyResolver;
			//weaver.AssemblyFilePath = SolutionDirectoryPath + outputDllPath + _module;
			//weaver.WriteModule();

			//// read again (discard our changes locally)
			//InnerWeaver weaver = (InnerWeaver)AssemblyResolver;
			//weaver.AssemblyFilePath = AssemblyFilePath;
			//weaver.ReadModule();
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
		void generateWrappers()
		{
			DirectoryInfo wrapperDirectory = new DirectoryInfo(SolutionDirectoryPath + outputWrapperPath);
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
		void removeReferenceToMscorlib40( ModuleDefinition _module )
		{
			// remove reference to mscorlib 4.0 (added by Fody?)
			var mscorlib4 = _module.AssemblyReferences.FirstOrDefault(a => a.Version.Major == 4 && a.Name == "mscorlib");
			if (mscorlib4 != null)
			{
				LogWarning("removeReferenceToMscorlib40 in " + _module);
				_module.AssemblyReferences.Remove(mscorlib4);
			}
		}
	}
}
