using System;
using System.Reflection;
using System.Reflection.Emit;

namespace AsyncRpc.CodeGen
{
	internal static class Generator
	{
		private static readonly AssemblyBuilder Asm = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("AsyncRpc.Generated"),
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
				return tb.CreateType();
			}
		}


	}
}
