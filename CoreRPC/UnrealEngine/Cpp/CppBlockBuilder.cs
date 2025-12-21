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

    public virtual void BlockStart()
    {
        AppendLine("{");
        AddSpace();
    }

    public void AddSpace()
    {
        _currentSpace.Append("    ");
    }

    public void RemoveSpace()
    {
        if(_currentSpace.Length >= 4) _currentSpace.Remove(_currentSpace.Length - 4, 4);
    }
    
    public virtual void BlockEnd()
    {
        RemoveSpace();
        AppendLine("};");
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