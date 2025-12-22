using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CoreRPC.Binding;

namespace CoreRPC.UnrealEngine;

public class UeTypeConverter
{
    private readonly UeCodeGenOptions _options;
    private readonly IMethodBinder _methodBinder;
    private readonly List<UeTypeDescriptor> _registeredTypes = new();
    private readonly Dictionary<Type, UeTypeDescriptor> _typeMap = new();

    public List<UeTypeDescriptor> RegisteredTypes => _registeredTypes.Where(x => x.IsStruct || x.IsEnum).ToList();

    public UeTypeConverter(UeCodeGenOptions options)
    {
        _options = options;
    }
        
    public void AddBase<T>(string typeName, bool blueprintable)
    {
        var t = new UeTypeDescriptor()
        {
            NetType = typeof(T),
            BaseType = null,
            BlueprintType = blueprintable,
            UeTypeName = typeName,
            Properties = {  }
        };
        _registeredTypes.Add(t);
        _typeMap.Add(typeof(T), t);
    }

    public void AddRpcType(Type t)
    {
        if (_typeMap.ContainsKey(t)) return;
        var typeDescriptor = new UeTypeDescriptor()
        {
            NetType = t,
            UeTypeName = ConvertTypeName(t.IsInterface ? t.Name.Substring(1) : t.Name, "FCoreRpcProxy"),
            BlueprintType = false,
            BaseType = GetOrRegister(typeof(NullTypeForBaseTypes),false)
        };
        foreach (var methodInfo in t.GetMethods())
        {
            if (methodInfo.DeclaringType == typeof(object)) continue;
            var methodDesc = new UeMethodDescriptor()
            {
                MethodInfo = methodInfo,
                MethodName = methodInfo.Name,
            };
            methodDesc.ReturnType = methodInfo.ReturnType;
            if (methodInfo.ReturnType == typeof(void))
            {
                methodDesc.ReturnType = null;
                methodDesc.ReturnTypeIsTask = false;
            }

            if (methodDesc.ReturnType != null)
            {
                if (methodInfo.ReturnType.IsConstructedGenericType &&
                    methodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    methodDesc.ReturnType = methodInfo.ReturnType.GetGenericArguments()[0];
                    methodDesc.ReturnTypeIsTask = true;
                }
            }
            methodDesc.Parameters = methodInfo.GetParameters().ToDictionary(x => x.Name, x => x.ParameterType);
            typeDescriptor.Methods.Add(methodDesc);
        }
        _registeredTypes.Add(typeDescriptor);
        _typeMap.Add(t, typeDescriptor);
    }

    public UeTypeDescriptor GetOrRegister(Type t, bool blueprintType = true)
    {
        if (_typeMap.TryGetValue(t, out var descriptor)) return descriptor;
        if (t.IsArray)
        {
            var elType = GetOrRegister(t.GetElementType(), blueprintType);
            return new UeTypeDescriptor()
            {
                UeTypeName = $"TArray<{elType.UeTypeName}>",
                BaseType = null,
                BlueprintType = blueprintType,
                NetType = t,
            };
        }

        if (t.IsConstructedGenericType)
        {
            var typeArg = t.GetGenericArguments()[0];
            if (t.GetGenericTypeDefinition() == typeof(List<>) ||
                t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                var typeArgDesc = GetOrRegister(typeArg, blueprintType);
                return new UeTypeDescriptor()
                {
                    UeTypeName = $"TArray<{typeArgDesc.UeTypeName}>",
                    BaseType = null,
                    BlueprintType = blueprintType,
                    NetType = t,
                };
            }
            if (t.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var typeArgDesc = GetOrRegister(typeArg, blueprintType);
                return new UeTypeDescriptor()
                {
                    UeTypeName = $"{_options.FutureClassName}<{typeArgDesc.UeTypeName}>",
                    BaseType = null,
                    BlueprintType = false,
                    NetType = t,
                };
            }
        }

        var typeDescriptor = new UeTypeDescriptor()
        {
            NetType = t,
            UeTypeName = ConvertTypeName(t.Name, t.IsEnum ? "E" : "F"),
            BlueprintType = blueprintType
        };
        if (t.IsEnum)
        {
            var names = Enum.GetNames(t);
            var values = Enum.GetValues(t);
            for (int i = 0; i < names.Length; i++)
                typeDescriptor.EnumValues.Add(names[i], (int)values.GetValue(i));
        }
        else
        {
            if (t.BaseType != typeof(object) && t.BaseType != typeof(Array) && (t?.BaseType?.IsClass ?? false))
            {
                typeDescriptor.BaseType = GetOrRegister(t.BaseType, blueprintType);
            }

            if (t.IsConstructedGenericType)
            {
                var typeArg = t.GetGenericArguments()[0];
                typeDescriptor.UeTypeName = ConvertGenericTypeName(t.Name, typeArg.Name);
            }

            if (t.IsArray)
            {
                var typeArgDesc = GetOrRegister(t.GetElementType(), blueprintType);
                typeDescriptor.UeTypeName = $"TArray<{typeArgDesc.UeTypeName}>";
            }

            var typeProps = t.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (var prop in typeProps.Where(x =>
                         (x.GetMethod?.IsPublic ?? false)))
            {
                if(prop.DeclaringType != t) continue;
                var d = GetOrRegister(prop.PropertyType);
                typeDescriptor.Properties.Add(new UePropertyDescriptor()
                {
                    NetType = prop.PropertyType,
                    BlueprintEditable = blueprintType,
                    PropertyName = ConvertPropertyName(prop.Name)
                });
            }
        }

        _registeredTypes.Add(typeDescriptor);
        _typeMap.Add(t, typeDescriptor);
        return typeDescriptor;
    }

    private string ConvertGenericTypeName(string tName, string typeArgName)
    {
        var argStart = tName.IndexOf('`');
        return $"F{tName.Substring(0, argStart)}_{typeArgName}";
    }

    private string ConvertTypeName(string netTypeName, string prefix) => $"{prefix}{netTypeName.Substring(0, 1).ToUpper()}{netTypeName.Substring(1)}";
    private string ConvertPropertyName(string netTypeName) => $"{netTypeName.Substring(0, 1).ToUpper()}{netTypeName.Substring(1)}";
}

public class UeCodeGenOptions
{
    public Type GenericTypeForResult { get; set; }
    public string ApiDefine { get; set; }
    public string RpcClientBaseType { get; set; }
    public string FutureClassName { get; set; }
    public string DtoHeaderName { get; set; }
    
    public string[] Includes { get; set; }
}