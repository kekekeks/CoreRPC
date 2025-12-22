using System.Reflection;
using CoreRPC.AspNetCore;
using CoreRPC.Binding.Default;
using CoreRPC.UnrealEngine;

namespace CoreRpcUeTestApp;

public class UeRpcCodeGen
{
    public static void GenerateCode(string path, Assembly apiAssembly)
    {
        var options = new UeCodeGenOptions()
        {
            DtoHeaderName = "CommunicationDto",
            FutureClassName = "SD::TExpectedFuture",
            RpcClientBaseType = "FCoreRpcClientBase"
        };
        var codeGen = new UeRpcGenerator(path, options, new DefaultMethodBinder());
        Directory.CreateDirectory(path);
        foreach (var type in apiAssembly.GetTypes().Where(x => x.IsClass && !x.IsAbstract))
        {
            var attr = type.GetCustomAttribute<RegisterRpcAttribute>();
            if (attr != null)
            {
                var interfaceType = attr.Interface;
                codeGen.TypeConverter.AddRpcType(interfaceType);
                var className = $"FCoreRpcProxy{interfaceType.Name.Substring(1)}";
                var header = codeGen.GenerateHeaderForRpc(interfaceType, className);
                File.WriteAllText(Path.Combine(path, $"{className}.h"), header);
                var code = codeGen.GenerateCodeForRpc(interfaceType, className);
                File.WriteAllText(Path.Combine(path, $"{className}.cpp"), code);
            }
        }
        var commDto = codeGen.GenerateHeaderWithDto(options.DtoHeaderName);
        File.WriteAllText(Path.Combine(path, $"{options.DtoHeaderName}.h"), commDto);
        var cppFile = "#pragma once" + Environment.NewLine + $"#include \"{options.DtoHeaderName}.h\"";
        File.WriteAllText(Path.Combine(path, $"{options.DtoHeaderName}.cpp"), cppFile);
    }
}