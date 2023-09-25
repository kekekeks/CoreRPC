using System;
using System.Collections.Generic;
using System.Reflection;
using CoreRPC.Binding;
using CoreRPC.Binding.Default;
using CoreRPC.Routing;

namespace CoreRPC.Typescript
{
    public class TypescriptGenerationOptions
    {
        public static string ToCamelCase(string s) => char.ToLower(s[0]) + s.Substring(1);
        public Func<Type, string> ApiFieldNamingPolicy { get; set; } = t => ToCamelCase(t.Name);
        public Func<string, string> ApiMethodNamingPolicy { get; set; } = ToCamelCase;
        public Func<Type, string> DtoClassNamingPolicy { get; set; } = t => t.Name;
        public Func<string, string> DtoFieldNamingPolicy { get; set; } = s => s;
        public IMethodBinder Binder { get; set; } = new DefaultMethodBinder();
        public ITargetNameExtractor TargetNameExtractor { get; set; } = new DefaultTargetNameExtractor();
        public string ClassName { get; set; } = "CoreApi";
        public Func<Type, Type> CustomTypeMapping { get; set; } = null;
        public CustomTsTypeMapping CustomTsTypeMapping { get; set; } = null;
        public List<Type> AdditionalTypes { get; set; } = new();
    }

    public delegate string CustomTsTypeMapping(Type type, Func<Type, string> subTypeMapper);
}