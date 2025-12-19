namespace CoreRPC.UnrealEngine.Cpp;

public class CppStructBuilder : CppBlockBuilder
{
    public CppStructBuilder(string name, bool blueprintType, string currentSpace, string baseType = null) : base(currentSpace)
    {
        var macro = blueprintType ? "BlueprintType" : "";
        AppendLine($"USTRUCT({macro})");
        macro = baseType != null ? $" : public {baseType}" : "";
        AppendLine($"struct {name}{macro}");
        BlockStart();
        AppendLine("GENERATED_BODY()");
        AppendLine("");
    }

    public CppStructBuilder AppendProperty(string propertyTypeName, string propertyName, bool blueprintEditable)
    {
        var macro = blueprintEditable ? "EditAnywhere, BlueprintReadWrite" : "";
        AppendLine($"UPROPERTY({macro})");
        AppendLine($"{propertyTypeName} {propertyName};");
        return this;
    }
}