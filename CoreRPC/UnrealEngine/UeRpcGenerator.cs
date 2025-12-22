using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreRPC.Binding;
using CoreRPC.UnrealEngine.Cpp;

namespace CoreRPC.UnrealEngine;

public class NullTypeForBaseTypes
{
    
}

public class UeRpcGenerator
{
    private readonly string _path;
    private readonly UeCodeGenOptions _options;
    private readonly IMethodBinder _methodBinder;

    public UeTypeConverter TypeConverter { get; }

    public UeRpcGenerator(string path, UeCodeGenOptions options, IMethodBinder methodBinder)
    {
        _path = path;
        _options = options;
        _methodBinder = methodBinder;
        TypeConverter = new UeTypeConverter(options);
        TypeConverter.AddBase<string>("FString", true);
        TypeConverter.AddBase<int>("int", true);
        TypeConverter.AddBase<bool>("bool", true);
        TypeConverter.AddBase<short>("int", true);
        TypeConverter.AddBase<byte>("int", true);
        TypeConverter.AddBase<float>("float", true);
        TypeConverter.AddBase<double>("double", true);
        TypeConverter.AddBase<decimal>("double", true);
        TypeConverter.AddBase<long>("int", true);
        TypeConverter.AddBase<NullTypeForBaseTypes>(options.RpcClientBaseType, false);
    }

    public string GenerateHeaderForRpc(Type rpcType, string headerName)
    {
        var headerGenerator = new UeHeaderFileGenerator(headerName, false, TypeConverter, _options, new[]
        {
            $"{_options.DtoHeaderName}.h",
        }.Concat(_options.Includes ?? []).Concat(new[]
        {
            "../CoreRpc/CoreRpcClientBase.h"
        }).ToArray());
        headerGenerator.AddType(rpcType);
        return headerGenerator.BuildHeader();
    }

    public string GenerateCodeForRpc(Type rpcType, string headerName)
    {
        var typeInfo = TypeConverter.GetOrRegister(rpcType);
        var codeBuilder = new CppClassImplementationBuilder(typeInfo.UeTypeName, "", new[]
        {
            "../CoreRpc/CoreRpcClientBase.h",
        }.Concat(_options.Includes ?? []).ToArray());
        codeBuilder.BeginConstructor(new Dictionary<string, string>()
        {
            { "Url", "FString" },
            { "Auth", "FString" }
        }, _options.RpcClientBaseType, new[] { "Url", "Auth" });
        codeBuilder.EndMethod();
        foreach (var method in typeInfo.Methods)
        {
            var retType = method.ReturnType != null ? TypeConverter.GetOrRegister(method.ReturnType) : null;
            var retTypeStr = retType != null
                ? $"{_options.FutureClassName}<{retType.UeTypeName}>"
                : $"{_options.FutureClassName}<void>";
            var argList =
                method.Parameters.ToDictionary(x => x.Key, x => TypeConverter.GetOrRegister(x.Value).UeTypeName);
            codeBuilder.BeginMethod(method.MethodName, retTypeStr, argList);
            var firstLine = new StringBuilder();
            firstLine.Append("return ");
            firstLine.Append("SendRequest");
            firstLine.Append($"<{retType?.UeTypeName ?? "void"}>");
            firstLine.Append("(FMethodCallBuilder()");
            codeBuilder.AppendLine(firstLine.ToString());
            codeBuilder.AddSpace();
            codeBuilder.AppendLine($".Target(\"{rpcType.Name}\")");
            var signature = Convert.ToBase64String(_methodBinder.GetMethodSignature(method.MethodInfo));
            codeBuilder.AppendLine($".MethodSignature(\"{signature}\")");
            foreach (var methodParameter in method.Parameters)
            {
                codeBuilder.AppendLine($".Argument({methodParameter.Key})");
            }
            codeBuilder.RemoveSpace();
            codeBuilder.AppendLine(");");
            codeBuilder.EndMethod();
        }
        return codeBuilder.Build();
    }

    public string GenerateHeaderWithDto(string name)
    {
        var headerGenerator = new UeHeaderFileGenerator(name, true, TypeConverter, _options);
        foreach (var type in TypeConverter.RegisteredTypes)
        {
            headerGenerator.AddType(type.NetType, true);
        }

        return headerGenerator.BuildHeader();
    }
}