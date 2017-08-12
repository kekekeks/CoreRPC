using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace CoreRPC.CodeGen
{
	public static class ProxyGen
	{

		static class TypeHolder<TInterface>
		{
// ReSharper disable StaticFieldInGenericType
			public static readonly object SyncRoot = new object();
			public static Func<IRealProxy, TInterface> Constructor;
// ReSharper restore StaticFieldInGenericType
		}

		public static TInterface CreateInstance<TInterface>(IRealProxy proxy)
		{
			if (proxy == null)
				throw new ArgumentNullException();
			lock (TypeHolder<TInterface>.SyncRoot)
			{
				if (TypeHolder<TInterface>.Constructor == null)
				{
					TypeHolder<TInterface>.Constructor = Generate<TInterface>();
				}
				return TypeHolder<TInterface>.Constructor(proxy);
			}
		}

		private static readonly MethodInfo ListAddMethod = typeof (List<object>).GetMethod("Add", new[] {typeof (object)});
		private static readonly ConstructorInfo ListConstructor = typeof (List<object>).GetConstructor(Type.EmptyTypes);
		private static readonly MethodInfo ProxyInvoke = typeof (IRealProxy).GetMethod("Invoke");

		private static Func<IRealProxy, TInterface> Generate<TInterface>()
		{
			var iface = typeof (TInterface);

			var storedMethods = new Dictionary<string, MethodInfo>();

			var type = Generator.CreateType(iface.Name + ".Proxy", builder =>
				{
					builder.AddInterfaceImplementation(typeof (TInterface));
					var proxy = builder.DefineField("_proxy", typeof (IRealProxy), FieldAttributes.Private);
					var ctorIl = builder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard,
					                                       new[] {typeof (IRealProxy)}).GetILGenerator();
					ctorIl.Emit(OpCodes.Ldarg_0);
					ctorIl.Emit(OpCodes.Ldarg_1);
					ctorIl.Emit(OpCodes.Stfld, proxy);
					ctorIl.Emit(OpCodes.Ret);

					foreach (var ifaceMethodInfo in iface.GetMethods())
					{
						var method = builder.DefineMethod(ifaceMethodInfo.Name, MethodAttributes.Public | MethodAttributes.Virtual, ifaceMethodInfo.ReturnType,
						                                  ifaceMethodInfo.GetParameters().Select(p => p.ParameterType).ToArray());
						var methodIl = method.GetILGenerator();
						
						//var list = new List<object>();
						var locList = methodIl.DeclareLocal(typeof (List<object>));
						methodIl.Emit(OpCodes.Newobj, ListConstructor);
						methodIl.Emit(OpCodes.Stloc, locList);
						
						for (var c = 0; c < ifaceMethodInfo.GetParameters().Count(); c++)
						{
							//list.Add(__arg[c])
							methodIl.Emit(OpCodes.Ldloc, locList);
							methodIl.Emit(OpCodes.Ldarg, c + 1);
							if (ifaceMethodInfo.GetParameters()[c].ParameterType.GetTypeInfo().IsValueType)
								methodIl.Emit (OpCodes.Box, ifaceMethodInfo.GetParameters ()[c].ParameterType);
							methodIl.Emit(OpCodes.Castclass, typeof(object));
							methodIl.Emit(OpCodes.Call, ListAddMethod);
						}

						var mnfoFieldName = "method_" + ifaceMethodInfo.Name + "_" + Guid.NewGuid().ToString("N");
						var mnfoField = builder.DefineField (mnfoFieldName, typeof (MethodInfo), FieldAttributes.Private | FieldAttributes.Static);
						storedMethods[mnfoFieldName] = ifaceMethodInfo;

						//return _proxy.Invoke(__methodInfo, list)
						methodIl.Emit (OpCodes.Ldarg_0);
						methodIl.Emit(OpCodes.Ldfld, proxy);
						methodIl.Emit(OpCodes.Ldsfld, mnfoField);
						methodIl.Emit(OpCodes.Ldloc, locList);
						methodIl.Emit(OpCodes.Castclass, typeof (IEnumerable));
						
						methodIl.Emit (OpCodes.Call, ProxyInvoke);

						if (ifaceMethodInfo.ReturnType != typeof (void))
						{
							methodIl.Emit(OpCodes.Unbox_Any, ifaceMethodInfo.ReturnType);
						}
						else
							methodIl.Emit(OpCodes.Pop);


						methodIl.Emit(OpCodes.Ret);

						builder.DefineMethodOverride(method, ifaceMethodInfo);
					}
					
				});

			foreach (var storedMethod in storedMethods)
				type.GetField(storedMethod.Key, BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, storedMethod.Value);

			var ctor = type.GetConstructors()[0];
			var arg = Expression.Parameter(typeof (IRealProxy), "proxy");
			return Expression.Lambda<Func<IRealProxy, TInterface>>(Expression.Convert(Expression.New(ctor, arg), typeof (TInterface)), arg).Compile();
		}
	}
}
