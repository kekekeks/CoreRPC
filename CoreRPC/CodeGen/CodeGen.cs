using System;
using System.Reflection;
using System.Reflection.Emit;

namespace CoreRPC.CodeGen
{
	internal static class Generator
	{
		private static readonly AssemblyBuilder Asm = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("CoreRPC.Generated"),
		                                                                            AssemblyBuilderAccess.Run);

		private static readonly ModuleBuilder Builder = Asm.DefineDynamicModule("Module");
		private static readonly object SyncRoot = new object();



		public static Type CreateType(string name, Action<TypeBuilder> builder)
		{
			name += "." + Guid.NewGuid().ToString("N");
			lock (SyncRoot)
			{
				var tb = Builder.DefineType(name);
				builder(tb);
				return tb.CreateTypeInfo().AsType();
			}
		}


	}
}
