using System.Collections.Generic;
using System.Linq;

namespace CoreRPC.UnrealEngine.Cpp;


public class CppClassImplementationBuilder : CppBlockBuilder
{
    private readonly string _className;

    public CppClassImplementationBuilder(string className, string currentSpace, string[] additionalIncludes) : base(currentSpace)
    {
        _className = className;
        AppendLine($"#include \"{className}.h\"");
        if (additionalIncludes?.Length > 0)
        {
            foreach (var include in additionalIncludes)
            {
                AppendLine($"#include \"{include}\"");
            }
        }
        AppendLine("");
    }

    public override void BlockEnd()
    {
        
    }

    public void BeginMethod(string methodName, string returnType, Dictionary<string, string> args)
    {
        var methodHead = $"{returnType} {_className}::{methodName}(";
        methodHead += string.Join(", ", args.Select(x => $"{x.Key} {x.Value}"));
        methodHead += ")";
        AppendLine(methodHead);
        BlockStart();
    }

    public void EndMethod()
    {
        RemoveSpace();
        AppendLine("}");
    }
}