using System;
using System.Collections.Generic;
using System.Text;
using CoreRPC.UnrealEngine.Cpp;

namespace CoreRPC.UnrealEngine;

public class UeHeaderFileGenerator
{
    private readonly string _headerName;
    private readonly bool _needGeneratedType;
    private readonly UeTypeConverter _typeConverter;
    private readonly UeCodeGenOptions _options;

    private StringBuilder _header = new StringBuilder();

    private List<UeTypeDescriptor> _addedTypes = new List<UeTypeDescriptor>();
    
    public UeHeaderFileGenerator(string headerName, bool needGeneratedType, 
        UeTypeConverter typeConverter, 
        UeCodeGenOptions options, string[] additionalIncludes = null)
    {
        _headerName = headerName;
        _needGeneratedType = needGeneratedType;
        _typeConverter = typeConverter;
        _options = options;
        _header.AppendLine($"// CoreRPC Unreal Engine generated header {headerName}");
        _header.AppendLine("#pragma once");
        _header.AppendLine();
        _header.AppendLine("#include \"CoreMinimal.h\"");
        if (additionalIncludes?.Length > 0)
        {
            foreach (var include in additionalIncludes)
            {
                _header.AppendLine($"#include \"{include}\"");
            }
        }
        if (needGeneratedType)
        {
            _header.AppendLine($"#include \"{headerName}.generated.h\"");
        }
        _header.AppendLine();
    }

    public void AddType(Type t, bool blueprintType = true)
    {
        var currentType = new StringBuilder();
        var deferredTypes = new List<UeTypeDescriptor>();
        var typeDescriptor = _typeConverter.GetOrRegister(t, blueprintType);
        if (_addedTypes.Contains(typeDescriptor)) return;
        if (typeDescriptor.IsStruct)
        {
            var builder =
                new CppStructBuilder(typeDescriptor.UeTypeName, blueprintType, "", typeDescriptor.BaseType?.UeTypeName);
            foreach (var property in typeDescriptor.Properties)
            {
                var propertyType = _typeConverter.GetOrRegister(property.NetType, blueprintType);
                if (!_addedTypes.Contains(propertyType))
                {
                    if (propertyType.IsClass || propertyType.IsStruct || propertyType.IsEnum)
                    {
                        deferredTypes.Add(propertyType);
                    }
                }
                builder.AppendProperty(propertyType.UeTypeName, property.PropertyName, property.BlueprintEditable);
            }
            currentType.AppendLine(builder.Build());
        }
        else if (typeDescriptor.IsEnum)
        {
            var builder = new CppEnumBuilder(typeDescriptor.UeTypeName, "");
            foreach (var enumValue in typeDescriptor.EnumValues)
            {
                builder.AppendValue(enumValue.Key, enumValue.Value.ToString());
            }
            currentType.Append(builder.Build());
        }
        else if (typeDescriptor.IsClass)
        {
            var builder = new CppNativeClassHeaderBuilder(typeDescriptor.UeTypeName, "", 
                typeDescriptor.BaseType != null ? new []{typeDescriptor.BaseType.UeTypeName} : null);
            builder.AppendLine($"friend class FCoreRpcEngine;");
            builder.AddConstructor(new Dictionary<string, string>()
            {
                { "Url" , "FString" },
                { "Auth", "FString" }
            }, CppVisibilityScope.Private);
            foreach (var method in typeDescriptor.Methods)
            {
                var args = new Dictionary<string, string>();
                foreach (KeyValuePair<string, Type> arg in method.Parameters)
                {
                    var argTypeDesc = _typeConverter.GetOrRegister(arg.Value, true);
                    args.Add(arg.Key, argTypeDesc.UeTypeName);
                }
                var retType = method.ReturnType != null ? _typeConverter.GetOrRegister(method.ReturnType) : null;
                var retTypeStr = retType != null ? $"{_options.FutureClassName}<{retType.UeTypeName}>" : "void";
                builder.AddMethod(method.MethodName, retTypeStr, CppVisibilityScope.Public, args,
                    false, false);
            }

            foreach (var field in typeDescriptor.Properties)
            {
                var tField = _typeConverter.GetOrRegister(field.NetType, true);
                builder.AddField(tField.UeTypeName, field.PropertyName, CppVisibilityScope.Private);
            }
            currentType.Append(builder.Build());

        }
        _addedTypes.Add(typeDescriptor);
        foreach (var type in deferredTypes)
        {
            AddType(type.NetType, blueprintType);
        }
        _header.AppendLine($"// CoreRpcGeneratedType: {typeDescriptor.UeTypeName}");
        _header.AppendLine(currentType.ToString());
    }
    
    public string BuildHeader() => _header.ToString();
}