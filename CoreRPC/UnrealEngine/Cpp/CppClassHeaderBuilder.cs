using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CoreRPC.UnrealEngine.Cpp;

public enum CppVisibilityScope
{
    Public = 0,
    Protected = 1,
    Private = 2
}

public class CppNativeClassHeaderBuilder : CppBlockBuilder
{
    private readonly string _name;

    private CppVisibilityScope _visibilityScope;
    
    public CppNativeClassHeaderBuilder(string name, string currentSpace, string[] baseTypes = null) : base(currentSpace)
    {
        _name = name;
        var header = $"class {name}";
        if(baseTypes != null && baseTypes.Length > 0)
            header += $" : {string.Join(", ", baseTypes.Select(x => $"public {x}"))}";
        AppendLine(header);
        BlockStart();
        RemoveSpace();
        AppendLine("public:");
        AddSpace();
        _visibilityScope = CppVisibilityScope.Public;
        AppendLine("");
    }

    private string GetScopeLine(CppVisibilityScope visibilityScope) => visibilityScope switch
    {
        CppVisibilityScope.Public => "public:",
        CppVisibilityScope.Private => "private:",
        CppVisibilityScope.Protected => "protected:",
        _ => ""
    };

    private void CheckScope(CppVisibilityScope scope)
    {
        if (_visibilityScope == scope) return;
        AppendLine(GetScopeLine(scope));
        _visibilityScope = scope;
    }

    public void AddConstructor(Dictionary<string, string> arguments, CppVisibilityScope visibility)
    {
        CheckScope(visibility);
        var methodInfo = $"{_name}(";
        methodInfo += string.Join(", ", arguments.Select(x => $"{x.Key} {x.Value}"));
        methodInfo += $")";
        methodInfo += ";";
        AppendLine(methodInfo);
    }

    public virtual void AddMethod(string name, string returnType, CppVisibilityScope visibility,
        Dictionary<string, string> arguments, bool isVirtual = false, bool isOverride = false)
    {
        CheckScope(visibility);
        var modifiers = isVirtual ? "virtual " : "";
        var methodInfo = $"{modifiers}{returnType} {name}(";
        methodInfo += string.Join(", ", arguments.Select(x => $"{x.Key} {x.Value}"));
        methodInfo += $")";
        if (isOverride) methodInfo += " override";
        methodInfo += ";";
        AppendLine(methodInfo);
    }

    public virtual void AddField(string typeName, string name, CppVisibilityScope visibility)
    {
        CheckScope(visibility);
        AppendLine($"{typeName} {name};");
    }
}