using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json.Linq;

namespace CoreRPC.Typescript
{
    class TypescriptTypeMapping
    {
        private readonly TypescriptGenerationOptions _opts;

        public TypescriptTypeMapping(TypescriptGenerationOptions opts)
        {
            _opts = opts;
        }

        private readonly Dictionary<Type, string> _processedTypes = new Dictionary<Type, string>();
        private readonly HashSet<string> _usedNames = new HashSet<string>();
        private readonly StringBuilder _builder = new StringBuilder();
        public override string ToString() => _builder.ToString();

        string MapComplexType(Type type)
        {
            if (_processedTypes.TryGetValue(type, out var name))
                return name;
            
            name = _opts.DtoClassNamingPolicy(type);
            name = name.Split('`')[0];
            if (_usedNames.Contains(name))
                throw new InvalidProgramException(name + " is already in use");
            _processedTypes[type] = name;

            var code = new TypescriptCodeBuilder();
            var typeInfo = type.GetTypeInfo();
            if (typeInfo.IsInterface || typeInfo.IsClass)
            {
                var tsName = name;
                if (typeInfo.IsGenericTypeDefinition)
                {
                    tsName = name +
                             "<" + string.Join(",", type.GetGenericArguments().Select(a => a.Name)) + ">";
                }

                code.BeginInterface(tsName);
                foreach (var p in type.GetProperties())
                {
                    if (p.GetAccessors(false).Any(x => x.IsStatic))
                        continue;

                    var typeName = MapType(p.PropertyType);
                    code.AppendInterfaceProperty(_opts.DtoFieldNamingPolicy(p.Name), typeName);
                }

                code.End();
            }
            else if (typeInfo.IsEnum)
            {
                code.BeginEnum(name);
                foreach (var en in Enum.GetNames(type))
                    code.AppendEnumValue(en, en);
                code.End();
            }
            else throw new InvalidProgramException($"Don't know how to convert {type.FullName} to typescript");

            _builder.AppendLine(code.ToString());
            return name;
        }

        string MapGenericType(Type type)
        {
            var def = MapComplexType(type.GetGenericTypeDefinition());
            return def + "<" + string.Join(",", type.GetGenericArguments().Select(MapType)) + ">";
        }

        string MapTypeNameInternal(Type t)
        {
            if (t.IsGenericParameter)
                return t.Name;
            
            var customName = _opts?.CustomTsTypeMapping?.Invoke(t, MapType);
            if (customName != null)
                return customName;
            var mappedType = _opts?.CustomTypeMapping?.Invoke(t);
            if (mappedType != null)
                return MapType(mappedType);
            
            var info = t.GetTypeInfo();
            if (info.IsGenericType && info.GetGenericTypeDefinition() == typeof(Nullable<>))
                return MapType(Nullable.GetUnderlyingType(t)) + " | null";
            if (typeof(JToken).IsAssignableFrom(t))
                return "any";
            if (t == typeof(object))
                return "object";
            if (t == typeof(string))
                return "string";
            if (t == typeof(void))
                return "void";
            if (info.IsPrimitive)
            {
                if (t == typeof(bool))
                    return "boolean";
                return "number";
            }

            var allInterfaces = t.GetInterfaces().Concat(new[] {t}).ToList();
            var dic = allInterfaces.FirstOrDefault(i =>
                i.IsConstructedGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));
            if (dic != null)
            {
                var ga = dic.GetGenericArguments().Select(MapType).ToList();
                return $"{{[key: {ga[0]}] : {ga[1]}}}";
            }
            
            var enumerable = allInterfaces.FirstOrDefault(i =>
                i.IsConstructedGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            if (enumerable != null)
            {
                return MapType(enumerable.GetGenericArguments()[0]) + "[]";
            }

            if (t.IsConstructedGenericType)
                return MapGenericType(t);
            
            return MapComplexType(t);
        }
        
        public string MapType(Type t)
        {
            if (_processedTypes.TryGetValue(t, out var name))
                return name;
            return _processedTypes[t] = MapTypeNameInternal(t);
        }
    }
}