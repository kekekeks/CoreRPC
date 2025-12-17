namespace CoreRPC.UnrealEngine.Cpp;

public class CppEnumBuilder : CppBlockBuilder
{
    public CppEnumBuilder(string name, string currentSpace) : base(currentSpace)
    {
        AppendLine("UENUM(BlueprintType)");
        AppendLine($"enum class {name} : uint8");
        BlockStart();
    }

    public CppEnumBuilder AppendValue(string name, string value)
    {
        AppendLine($"{name} = {value},");
        return this;
    }
}