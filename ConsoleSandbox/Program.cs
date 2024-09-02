using CoreRPC;
using CoreRPC.Binding.Default;
using CoreRPC.JsonLikeBinarySerializer;
using CoreRPC.Routing;
using CoreRPC.Transport;
using System.Text;


public static partial class Program
{
    public static void Main()
    {
        var engine = new Engine(new BinaryJsonLikeMethodCallSerializer(), new DefaultMethodBinder());
        var handler = engine.CreateProxy<IMyRpc>(
                new InternalThreadPoolTransport(engine.CreateRequestHandler(new Selector())));

        var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes("Stream1"));
        var memoryStreams = new List<Stream>
            {
                new MemoryStream(Encoding.UTF8.GetBytes("Stream2"))
            };
        var result = handler.Test(memoryStream, memoryStreams, new List<Memory<byte>>
            {
                new Memory<byte>(Encoding.UTF8.GetBytes("Memory1")),
                new Memory<byte>(Encoding.UTF8.GetBytes("Memory2"))
            }, Encoding.UTF8.GetBytes("Bytes")).Result;

        Console.WriteLine(result);
    }
}


class Selector : ITargetSelector
{
    public object GetTarget(string target, object callContext)
    {
        return new MyRpc();
    }
}