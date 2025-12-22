using System;
using System.Collections.Generic;
using System.IO;
using CoreRPC.Binding.Default;
using CoreRPC.Typescript;
using CoreRPC.UnrealEngine;
using Microsoft.AspNetCore.Hosting;

namespace CoreRPC.AspNetCore
{
    public class AspNetCoreRpcUnrealEngineCodeGenerator
    {
        public static void GenerateCode(string path, IHostingEnvironment env, Action<UeCodeGenOptions> configure = null)
        {
            GenerateCode(path, RpcTypesResolver.GetRpcTypes(env), configure);
        }
        
        public static void GenerateCode(string path, IEnumerable<Type> types, Action<UeCodeGenOptions> configure = null)
        {
            var options = new UeCodeGenOptions()
            {
                DtoHeaderName = "CommunicationDto",
                FutureClassName = "SD::TExpectedFuture",
                RpcClientBaseType = "FCoreRpcClientBase"
            };
            configure?.Invoke(options);
            var codeGen = new UeRpcGenerator(path, options, new DefaultMethodBinder());
            Directory.CreateDirectory(path);
            foreach (var type in types)
            {
                codeGen.TypeConverter.AddRpcType(type);
                var className = options.ClassNamePrefix + (type.IsInterface ? type.Name.Substring(1) : type.Name);
                var header = codeGen.GenerateHeaderForRpc(type, className);
                File.WriteAllText(Path.Combine(path, $"{className}.h"), header);
                var code = codeGen.GenerateCodeForRpc(type, className);
                File.WriteAllText(Path.Combine(path, $"{className}.cpp"), code);
            }
            var commDto = codeGen.GenerateHeaderWithDto(options.DtoHeaderName);
            File.WriteAllText(Path.Combine(path, $"{options.DtoHeaderName}.h"), commDto);
            var cppFile = $"#include \"{options.DtoHeaderName}.h\"";
            File.WriteAllText(Path.Combine(path, $"{options.DtoHeaderName}.cpp"), cppFile);
        }
    }
}