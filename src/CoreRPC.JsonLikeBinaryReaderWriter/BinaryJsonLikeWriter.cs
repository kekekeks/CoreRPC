using System.Buffers;
using System.Globalization;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace CoreRPC.JsonLikeBinaryReaderWriter;

public class BinaryJsonLikeWriter : JsonWriter
{
    private readonly BinaryWriter _writer;
    private readonly Stream _stream;
    private readonly bool _leaveOpen;

    public BinaryJsonLikeWriter(Stream stream, bool leaveOpen = false)
    {
        _stream = stream;
        _leaveOpen = leaveOpen;
        _writer = new BinaryWriter(stream);
    }
    
    public override void Flush() => _writer.Flush();

    public override void Close()
    {
        base.Close();
        Flush();
        if (CloseOutput && !_leaveOpen)
            _writer.Close();
    }

    void WriteTokenType(JsonToken token) => _writer.Write((int)token);

    public override void WriteStartObject()
    {
        base.WriteStartObject();
        WriteTokenType(JsonToken.StartObject);
    }

    public override void WriteStartArray()
    {
        base.WriteStartArray();
        WriteTokenType(JsonToken.StartArray);
    }

    public override void WriteStartConstructor(string name)
    {
        base.WriteStartConstructor(name);
        WriteTokenType(JsonToken.StartConstructor);
        _writer.Write(name);
    }

    protected override void WriteEnd(JsonToken token)
    {
        base.WriteEnd(token);
        WriteTokenType(token);
    }

    public override void WritePropertyName(string name)
    {
        base.WritePropertyName(name);
        WriteTokenType(JsonToken.PropertyName);
        _writer.Write(name);
    }

    public override void WriteNull()
    {
        base.WriteNull();
        WriteTokenType(JsonToken.Null);
    }

    public override void WriteUndefined()
    {
        base.WriteUndefined();
        WriteTokenType(JsonToken.Undefined);
    }

    public override void WriteRaw(string json)
    {
        base.WriteRaw(json);
        WriteTokenType(JsonToken.Raw);
        _writer.Write(json);
    }

    public override void WriteRawValue(string json) => throw new NotSupportedException();

    public override void WriteValue(string value)
    {
        base.WriteValue(value);
        WriteTokenType(JsonToken.String);
        _writer.Write(value);
    }

    void WriteIntegerValue(long value)
    {
        WriteTokenType(JsonToken.Integer);
        _writer.Write(value);
    }

    public override void WriteValue(int value)
    {
        base.WriteValue(value);
        WriteIntegerValue(value);
    }
    
    public override void WriteValue(uint value)
    {
        base.WriteValue(value);
        WriteIntegerValue(value);
    }
    
    public override void WriteValue(long value)
    {
        base.WriteValue(value);
        WriteIntegerValue(value);
    }
    
    public override void WriteValue(short value)
    {
        base.WriteValue(value);
        WriteIntegerValue(value);
    }
    
    public override void WriteValue(ushort value)
    {
        base.WriteValue(value);
        WriteIntegerValue(value);
    }
    
    public override void WriteValue(byte value)
    {
        base.WriteValue(value);
        WriteIntegerValue(value);
    }
    
    public override void WriteValue(sbyte value)
    {
        base.WriteValue(value);
        WriteIntegerValue(value);
    }
    
    public override void WriteValue(ulong value)
    {
        base.WriteValue(value);
        WriteIntegerValue((long)value);
    }

    void WriteFloatValue(double value)
    {
        WriteTokenType(JsonToken.Float);
        _writer.Write(value);
    }
    
    public override void WriteValue(float value)
    {
        base.WriteValue(value);
        WriteFloatValue(value);
    }
    
    public override void WriteValue(decimal value)
    {
        base.WriteValue(value);
        WriteFloatValue((double)value);
    }
    
    public override void WriteValue(double value)
    {
        base.WriteValue(value);
        WriteFloatValue(value);
    }

    public override void WriteValue(bool value)
    {
        base.WriteValue(value);
        WriteTokenType(JsonToken.Boolean);
        _writer.Write(value);
    }
    
    public override void WriteValue(char value)
    {
        base.WriteValue(value);
        WriteTokenType(JsonToken.String);
        _writer.Write(new string(value, 1));
    }

    void WriteDate(DateTimeOffset value)
    {
        WriteTokenType(JsonToken.Date);
        _writer.Write(value.Ticks);
        _writer.Write(value.Offset.Ticks);
    }

    public override void WriteValue(DateTime value)
    {
        base.WriteValue(value);
        WriteDate(new DateTimeOffset(value));
    }

    public override void WriteValue(DateTimeOffset value)
    {
        base.WriteValue(value);
        WriteDate(value);
    }

    public override void WriteValue(byte[] value)
    {
        if (value == null)
        {
            WriteNull();
            return;
        }
        
        base.WriteValue(value);
        WriteTokenType(JsonToken.Bytes);
        _writer.Write(value.Length);
        _writer.Write(value);
    }

    public void WriteValue(ArraySegment<byte> segment)
    {
        base.WriteValue(Array.Empty<byte>());
        WriteTokenType(JsonToken.Bytes);
        _writer.Write(segment.Count);
        if (segment.Count > 0)
            _stream.Write(segment.Array, segment.Offset, segment.Count);
    }
    
#if NETCOREAPP3_1
    public void WriteValue(ReadOnlySpan<byte> span)
    {
        base.WriteValue(Array.Empty<byte>());
        WriteTokenType(JsonToken.Bytes);
        _writer.Write(span.Length);
        _writer.Write(span);
    }

    public void WriteValue(ReadOnlyMemory<byte> mem) => WriteValue(mem.Span);
#else
    public void WriteValue(ReadOnlySpan<byte> span)
    {
        var buf = ArrayPool<byte>.Shared.Rent(span.Length);
        span.CopyTo(buf);
        WriteValue(new ArraySegment<byte>(buf, 0, span.Length));
        ArrayPool<byte>.Shared.Return(buf);
    }

    public void WriteValue(ReadOnlyMemory<byte> mem)
    {
        if (MemoryMarshal.TryGetArray(mem, out var segment))
            WriteValue(segment);
        else
            WriteValue(mem.Span);
    }
#endif
    
    public void WriteValue(Stream s)
    {
        base.WriteValue(Array.Empty<byte>());
        WriteTokenType(JsonToken.Bytes);
        var size = (int)(s.Length - s.Position);
        _writer.Write(size);
        s.CopyTo(_stream);
    }

    public override void WriteValue(Guid value)
    {
        WriteValue(value.ToString("D", CultureInfo.InvariantCulture));
    }
    
    public override void WriteValue(TimeSpan value)
    {
        base.WriteValue(value.ToString(null, CultureInfo.InvariantCulture));
    }
    
    public override void WriteValue(Uri value)
    {
        if (value == null)
        {
            WriteNull();
            return;
        }
        WriteValue(value.OriginalString);
    }
}
