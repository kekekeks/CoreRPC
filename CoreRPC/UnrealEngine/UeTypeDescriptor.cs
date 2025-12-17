using System;
using System.Collections.Generic;

namespace CoreRPC.UnrealEngine;

public class UeTypeDescriptor
{
    public Type NetType { get; set; }
    public string UeTypeName { get; set; }
    public bool BlueprintType { get; set; }

    public UeTypeDescriptor BaseType { get; set; }

    public bool IsStruct => Properties.Count != 0 && Methods.Count == 0 && EnumValues.Count == 0;
    public bool IsEnum => EnumValues.Count != 0 && Properties.Count == 0 && Methods.Count == 0;
    public bool IsClass => Methods.Count != 0;
        
    public Dictionary<string, int> EnumValues { get; } = new Dictionary<string, int>();
    public List<UePropertyDescriptor> Properties { get; } = new List<UePropertyDescriptor>();
    
    public List<UeMethodDescriptor> Methods { get; } = new List<UeMethodDescriptor>();
}

public class UePropertyDescriptor
{
    public Type NetType { get; set; }
    public string PropertyName { get; set; }
    public bool BlueprintEditable { get; set; }
}

public class UeMethodDescriptor
{
    public string MethodName { get; set; }
    public Dictionary<string, string> Parameters { get; set; }
    public string ReturnType { get; set; }
    public bool ReturnTypeIsTask { get; set; }
}