using System;
using System.Collections.Generic;

namespace CoreRPC.UnrealEngine;

public class UeRpcGenerator
{
    private readonly string _path;
    private readonly UeCodeGenOptions _options;

    private readonly UeTypeConverter _typeConverter;
    
    public UeRpcGenerator(string path, UeCodeGenOptions options)
    {
        _path = path;
        _options = options;
        _typeConverter = new UeTypeConverter(options);
        _typeConverter.AddBase<string>("FString", true);
        _typeConverter.AddBase<int>("int", true);
        _typeConverter.AddBase<bool>("bool", true);
        _typeConverter.AddBase<short>("int", true);
        _typeConverter.AddBase<byte>("int", true);
        _typeConverter.AddBase<float>("float", true);
        _typeConverter.AddBase<double>("double", true);
    }

    public string GenerateHeaderWithDto(string name, IEnumerable<Type> types)
    {
        var headerGenerator = new UeHeaderFileGenerator(name, true, _typeConverter, _options);
        foreach (var type in types)
        {
            headerGenerator.AddType(type, true);
        }

        return headerGenerator.BuildHeader();
    }
}