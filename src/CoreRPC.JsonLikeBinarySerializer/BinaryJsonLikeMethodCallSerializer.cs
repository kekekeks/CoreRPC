using CoreRPC.Binding;
using CoreRPC.JsonLikeBinaryReaderWriter;
using CoreRPC.Routing;
using CoreRPC.Serialization;
using CoreRPC.Transferable;
using Newtonsoft.Json;

namespace CoreRPC.JsonLikeBinarySerializer;

public class BinaryJsonLikeMethodCallSerializer : JsonMethodCallSerializer
{
    public BinaryJsonLikeMethodCallSerializer() : base(new JsonSerializer()
    {
        Converters =
        {
            new BinaryJsonLikeStreamAndMemoryConverter(),
        }
    })
    {
        
    }

    public static void AppendConverters(JsonSerializer serializer)
    {
        foreach(var conv in serializer.Converters)
            if (conv is BinaryJsonLikeStreamAndMemoryConverter)
                return;
        serializer.Converters.Insert(0, new BinaryJsonLikeStreamAndMemoryConverter());
    }

    public BinaryJsonLikeMethodCallSerializer(JsonSerializer serializer, bool appendConverters) : base(serializer)
    {
        if (appendConverters)
            AppendConverters(serializer);

    }

    protected override JsonWriter CreateWriter(Stream stream) =>
        new BinaryJsonLikeWriter(stream, true);
    protected override JsonReader CreateReader(Stream stream) => 
        new BinaryJsonLikeReader(stream, new ArenaBinaryJsonLikeMemoryPool(null, null, null));

    class MethodCallWithArena : MethodCall
    {
        public ArenaBinaryJsonLikeMemoryPool Arena { get; } = new(null, null, null);
        public override void Dispose()
        {
            base.Dispose();
            Arena.Dispose();
        }
    }

    public override MethodCall DeserializeCall(Stream stream, IMethodBinder binder, ITargetSelector selector,
        object callContext)
    {
        var call = new MethodCallWithArena();
        var reader = new BinaryJsonLikeReader(stream, call.Arena);
        DeserializeCallCore(call, reader, binder, selector, callContext);
        return call;
    }
}