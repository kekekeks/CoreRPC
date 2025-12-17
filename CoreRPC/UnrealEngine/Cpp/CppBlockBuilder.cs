using System;
using System.Text;

namespace CoreRPC.UnrealEngine.Cpp;

public class CppBlockBuilder
{
    private readonly StringBuilder _currentSpace;
    protected readonly StringBuilder Builder = new StringBuilder(); 

    public CppBlockBuilder(string currentSpace)
    {
        _currentSpace = new StringBuilder(currentSpace);
    }

    public void BlockStart()
    {
        AppendLine("{");
        _currentSpace.Append("    ");
    }

    public void BlockEnd()
    {
        AppendLine("};");
        _currentSpace.Remove(_currentSpace.Length - 4, 4);
    }
    
    public string Build()
    {
        BlockEnd();
        return Builder.ToString();
    }

    public void AppendLine(string line)
    {
        Builder.Append(_currentSpace);
        Builder.Append(line);
        Builder.Append(Environment.NewLine);
    }
}