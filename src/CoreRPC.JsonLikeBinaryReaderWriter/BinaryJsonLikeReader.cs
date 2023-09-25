using System.Buffers;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json;

namespace CoreRPC.JsonLikeBinaryReaderWriter;

public class BinaryJsonLikeReader : JsonReader
{
    private readonly Stream _stream;
    private readonly IBinaryJsonLikeMemoryPool _pool;
    private readonly BinaryReader _reader;
    private JsonToken? _pushedFakeToken;
    private byte[] _tokenBuffer = new byte[4];

    public BinaryJsonLikeReader(Stream stream, IBinaryJsonLikeMemoryPool pool)
    {
        _stream = stream;
        _pool = pool;
        _reader = new BinaryReader(stream);
        DateParseHandling = DateParseHandling.None;
    }

    JsonToken ReadNextToken()
    {
        if (_pushedFakeToken.HasValue)
        {
            var rv = _pushedFakeToken.Value;
            _pushedFakeToken = null;
            return rv;
        }

        var len = 4;
        var offset = 0;
        while (len > 0)
        {
            var read = _stream.Read(_tokenBuffer, offset, len);
            if (read == 0)
                return JsonToken.None;
            len -= read;
            offset += read;
        }

        return (JsonToken)BitConverter.ToInt32(_tokenBuffer, 0);
    }

    public override bool Read()
    {
        if (Value is BytesReader lazyReader)
            lazyReader.Dispose();
        var type = ReadNextToken();

        if (type == JsonToken.None)
        {
            SetToken(JsonToken.None);
            return false;
        }
        else if (type is JsonToken.StartObject or JsonToken.EndObject or JsonToken.StartArray or JsonToken.EndArray or JsonToken.EndConstructor
            or JsonToken.Undefined or JsonToken.Null)
            SetToken(type);
        else if (type is JsonToken.StartConstructor or JsonToken.PropertyName or JsonToken.Comment or JsonToken.String or JsonToken.Raw)
            SetToken(type, _reader.ReadString());
        else if(type == JsonToken.Integer)
            // TODO: actual type?
            SetToken(type, _reader.ReadInt64());
        else if(type == JsonToken.Float)
            SetToken(type, _reader.ReadDouble());
        else if (type == JsonToken.Boolean)
            SetToken(type, _reader.ReadBoolean());
        else if (type == JsonToken.Date)
            SetToken(type, new DateTimeOffset(_reader.ReadInt64(), TimeSpan.FromTicks(_reader.ReadInt64())));
        else if (type == JsonToken.Bytes)
            SetToken(JsonToken.Bytes, new BytesReader(_stream, _reader.ReadInt32(), _pool));
        else
            throw new InvalidDataException("Unknown token: " + type);
        
        return true;
    }

    public override byte[] ReadAsBytes()
    {
        JsonToken type;
        // Skip comments
        while ((type = ReadNextToken()) == JsonToken.Comment)
        {
            
        }
        
        if (type == JsonToken.Bytes)
        {
            var len = _reader.ReadInt32();
            var bytes = _reader.ReadBytes(len);
            SetToken(type, bytes);
            return bytes;
        }

        _pushedFakeToken = type;
        
        return base.ReadAsBytes();
    }

    [TypeConverter(typeof(BytesReaderConverter))]
    public class BytesReader : IDisposable
    {
        private readonly Stream _s;
        private int _len;
        private readonly IBinaryJsonLikeMemoryPool _pool;
        private IMemoryOwner<byte>? _readMemory;
        private Stream? _readStream;
        private byte[]? _readBytes;

        public BytesReader(Stream s, int len, IBinaryJsonLikeMemoryPool pool)
        {
            _s = s;
            _len = len;
            _pool = pool;
        }
        
        public void Dispose()
        {
            // Need to skip
            if (_readStream == null && _readMemory == null && _readBytes == null && _len != 0)
            {
                var buf = ArrayPool<byte>.Shared.Rent(1024);
                while (_len > 0)
                {
                    var read = _s.Read(buf, 0, Math.Min(buf.Length, _len));
                    if (read == 0)
                    {
                        _len = 0;
                        return;
                    }

                    _len -= read;
                }
                ArrayPool<byte>.Shared.Return(buf);
            }
        }

        static void ReadExact(Stream s, ArraySegment<byte> segment)
        {
            if (segment.Count == 0)
                return;
            var offset = segment.Offset;
            var len = segment.Count;
            while (len > 0)
            {
                var read = s.Read(segment.Array, offset, len);
                if (read == 0)
                    throw new EndOfStreamException();
                offset += read;
                len -= read;
            }
        }
        
#if NETCOREAPP
        static void ReadExact(Stream s, Span<byte> span)
        {
            while (span.Length > 0)
            {
                var read = s.Read(span);
                if (read == 0)
                    throw new EndOfStreamException();
                span.Slice(read);
            }
        }
#endif

        class DummyMemoryOwner : IMemoryOwner<byte>
        {
            public DummyMemoryOwner(Memory<byte> memory)
            {
                Memory = memory;
            }
            public void Dispose()
            {
                
            }

            public Memory<byte> Memory { get; }
        }
        
        public IMemoryOwner<byte> ReadAsMemory()
        {
            if (_len == 0)
                return default;

            if (_readMemory != null)
                return _readMemory;
            if (_readBytes != null)
                return _readMemory = new DummyMemoryOwner(_readBytes);
            
            if (_readStream != null)
                throw new InvalidOperationException();

            var owner = _pool.RentMemory(_len);
            if (owner.Memory.Length != _len)
                owner = new SlicedMemoryOwner(owner, _len);
            var mem = owner.Memory;

            if (MemoryMarshal.TryGetArray(mem, out ArraySegment<byte> segment)) 
                ReadExact(_s, segment);
            else
            {
#if NETCOREAPP
                ReadExact(_s, mem.Span);
#else
                var tempArray = ArrayPool<byte>.Shared.Rent(_len);
                ReadExact(_s, new ArraySegment<byte>(tempArray, 0, _len));
                new Span<byte>(tempArray, 0, _len).CopyTo(mem.Span);
                ArrayPool<byte>.Shared.Return(tempArray);
#endif
            }
            return _readMemory = owner;
        }

        public Stream ReadAsStream()
        {
            if (_readStream != null)
                return _readStream;
            if (_readBytes != null)
                return _readStream = new MemoryStream(_readBytes);
            if (_readMemory != null)
                throw new InvalidOperationException();
            if (_len == 0)
                return _readStream = _pool.RentStream(0);

            var buffer = ArrayPool<byte>.Shared.Rent(1024);
            var ms = _pool.RentStream(_len);
            try
            {
                var len = _len;
                while (len > 0)
                {
                    var read = _s.Read(buffer, 0, Math.Min(buffer.Length, len));
                    if (read == 0)
                        throw new EndOfStreamException();
                    ms.Write(buffer, 0, read);
                    len -= read;
                }
            }
            catch
            {
                ms.Dispose();
                throw;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }

            ms.Position = 0;
            return _readStream = ms;
        }

        public byte[] ReadBytes()
        {
            if (_len == 0)
                return Array.Empty<byte>();
            if (_readBytes != null)
                return _readBytes;
            if (_readMemory != null && _readStream != null)
                throw new InvalidOperationException();
            var buf = new byte[_len];
            ReadExact(_s, new ArraySegment<byte>(buf, 0, _len));
            return _readBytes = buf;
        }
    }

    class BytesReaderConverter : TypeConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) =>
            destinationType == typeof(byte[]);

        public override object? ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(byte[]))
                return ((BytesReader)value)?.ReadBytes()!;
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}