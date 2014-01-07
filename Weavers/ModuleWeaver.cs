using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace Weavers
{
	public class ModuleWeaver
	{
		public ModuleDefinition ModuleDefinition { get; set; }

		const string BaseMonoBehaviour = "BaseMonoBehaviour";
		public void Execute()
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

			generateWrappers();


			// remove reference to mscorlib 4.0 (added by Fody?)
			var mscorlib4 = ModuleDefinition.AssemblyReferences.FirstOrDefault(a => a.Version.Major == 4 && a.Name == "mscorlib");
			if (mscorlib4 != null)
			{
				ModuleDefinition.AssemblyReferences.Remove(mscorlib4);
			}
		}

		void processBaseMonoBehaviour( TypeDefinition _type )
		{
			var update = _type.Methods.FirstOrDefault(m => m.Name == "Update");
			if (update != null)
			{
				update.Name = "dummy_Update";
				//_type.Methods.Remove(update);
			}
		}

		readonly List<TypeDefinition> typesToWrap = new List<TypeDefinition>();
		void processDerivedMonoBehaviour( TypeDefinition _type )
		{
			//var update = _type.Methods.FirstOrDefault(m => m.Name == "Update");
			//if (update != null)
			//{
			//	addLogMethod(_type, "hasUpdate_" + update.Body.CodeSize + "_" + update.Body.Instructions.Count);
			//}
			//else
			//{
			//	addLogMethod(_type, "noUpdate");
			//}

			if (!_type.IsAbstract)
			{
				typesToWrap.Add(_type);

				_type.IsAbstract = true;
			}
		}
		void addLogMethod( TypeDefinition _type, string _name )
		{
			TypeReference voidRef = ModuleDefinition.Import(typeof(void));
			_type.Methods.Add(new MethodDefinition(_name, MethodAttributes.Public, voidRef));
		}

		void generateWrappers()
		{
			DirectoryInfo wrapperDirectory = new DirectoryInfo(@"D:\Users\Benoit FOULETIER\Documents\GitHub\Unity.Fody\UnityProject\Assets\Script wrappers (autogen)");
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
	}
}
