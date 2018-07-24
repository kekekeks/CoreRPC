using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyModel;

namespace CoreRPC.AspNetCore
{
    static class RpcTypesResolver
    {
        public static IEnumerable<Type> GetRpcTypes(IHostingEnvironment env) =>
            GetRpcTypes(Assembly.Load(new AssemblyName(env.ApplicationName)));
        
        public static IEnumerable<Type> GetRpcTypes(Assembly entryAssembly)
        {
            var dctx = DependencyContext.Load(entryAssembly);
            foreach (var name in dctx.GetDefaultAssemblyNames())
            {
                Assembly asm;
                try
                {
                    asm = Assembly.Load(name);
                }
                catch
                {
                    continue;
                }
                foreach (var t in asm.DefinedTypes)
                    if ((!t.IsAbstract || t.IsInterface) && t.GetCustomAttribute<RegisterRpcAttribute>() != null)
                        yield return t;
            }
        }
    }
}