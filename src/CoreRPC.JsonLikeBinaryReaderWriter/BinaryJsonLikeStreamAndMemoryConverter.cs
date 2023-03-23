using System.Buffers;
using Microsoft.IO;
using Newtonsoft.Json;

namespace CoreRPC.JsonLikeBinaryReaderWriter;

public class BinaryJsonLikeStreamAndMemoryConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }
        var bwriter = (BinaryJsonLikeWriter)writer;
        if (value is ReadOnlyMemory<byte> rmem)
            bwriter.WriteValue(rmem);
        else if (value is Memory<byte> mem)
            bwriter.WriteValue(mem);
        else if (value is IMemoryOwner<byte> owner)
            bwriter.WriteValue(owner.Memory);
        else if (value is Stream stream)
            bwriter.WriteValue(stream);
        else throw new InvalidOperationException("Object type is not supported");
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;
        if (reader.TokenType != JsonToken.Bytes)
            throw new InvalidDataException("Expected Bytes token");
        var wrapper = (BinaryJsonLikeReader.BytesReader)reader.Value;
        if (objectType == typeof(ReadOnlyMemory<byte>) || objectType == typeof(Memory<byte>))
            return wrapper.ReadAsMemory().Memory;
        if (objectType == typeof(IMemoryOwner<byte>))
            return wrapper.ReadAsMemory();
        if (objectType.IsAssignableFrom(typeof(RecyclableMemoryStream)))
            return wrapper.ReadAsStream();
        throw new InvalidOperationException(objectType + " is not supported");
    }

    public override bool CanConvert(Type objectType) =>
        objectType == typeof(Memory<byte>)
        || objectType == typeof(ReadOnlyMemory<byte>)
        || typeof(IMemoryOwner<byte>).IsAssignableFrom(objectType)
        || typeof(Stream).IsAssignableFrom(objectType);
    
}

