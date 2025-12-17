using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CoreRPC.UnrealEngine;

public class UeTypeConverter
{
    private readonly UeCodeGenOptions _options;
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

    public UeTypeDescriptor GetOrRegister(Type t, bool blueprintType = true)
    {
        if (_typeMap.TryGetValue(t, out var descriptor)) return descriptor;
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
            if (t.BaseType != typeof(object) && (t?.BaseType?.IsClass ?? false))
            {
                typeDescriptor.BaseType = GetOrRegister(t.BaseType, blueprintType);
            }

            if (t.IsGenericType && t.GetGenericTypeDefinition() == _options.GenericTypeForResult)
            {
                var typeArg = t.GetGenericArguments()[0];
                typeDescriptor.UeTypeName = ConvertGenericTypeName(t.Name, typeArg.Name);
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
}