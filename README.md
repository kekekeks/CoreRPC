#AsyncRpc


Extensible library for WCF-like RPC fully compatible with Mono.

TCP transport supports connection pooling and multiplexing requests within one connection, infrastructure itself allows multiple "services" to be hosted inside one host. You may define your own handler factory or "routing" mechanism. Serializer (XmlSerializer is used by default) is also easy to replace.

##How to use

### Protocol definition
    public interface IService
    {
        Task<string> Foo(int);
    }

### Server

    class Service : IService
    {
        public Task<string> Foo(int bar)
        {
            return Task.FromResult(bar.ToString());
        }
    }
    
    static void StartServer()
    {
        var router = new DefaultTargetSelector();
        router.Register<IService, Service>();
        var host = new TcpHost(new Engine().CreateRequestHandler(router));
        host.StartListening(new IPEndPoint(IPAddress.Loopback, 9000));
    }

###Client
    var proxy = new Engine().CreateProxy<IService>(new TcpClientTransport("127.0.0.1", 9000));
    var res = await proxy.Foo(1);
