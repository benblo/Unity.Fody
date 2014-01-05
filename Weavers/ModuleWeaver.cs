using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace Weavers
{
	public class ModuleWeaver
	{
		public ModuleDefinition ModuleDefinition { get; set; }

		const string MonoBehaviour = "MonoBehaviour";
		public void Execute()
		{
			//ModuleDefinition.Types.Add(new TypeDefinition("MyNamespace", "MyType", TypeAttributes.Public, ModuleDefinition.Import(typeof(object))));

			TypeReference voidRef = ModuleDefinition.Import(typeof(void));

			foreach (var type in ModuleDefinition.Types)
			{
				//type.Methods.Add(new MethodDefinition("weavedIntoAll", MethodAttributes.Public, voidRef));

				if (type.Name == MonoBehaviour)
				{
					type.Methods.Add(new MethodDefinition("weavedIntoMonoBehaviour", MethodAttributes.Public, voidRef));
					continue;
				}

				if (type.BaseType != null &&
					type.BaseType.Name == MonoBehaviour)
				{
					type.Methods.Add(new MethodDefinition("weavedIntoScript", MethodAttributes.Public, voidRef));

					var update = type.Methods.FirstOrDefault(m => m.Name == "Update");
					if (update != null)
					{
						type.Methods.Add(new MethodDefinition("hasUpdate_" + update.Body.CodeSize, MethodAttributes.Public, voidRef));
					}
					else
						type.Methods.Add(new MethodDefinition("noUpdate", MethodAttributes.Public, voidRef));

					continue;
				}
			}
		}
	}
}
