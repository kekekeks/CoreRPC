# CoreRPC

Extensible library for WCF-like RPC targeting netstandard1.3 (compatible with .NET, Mono and .NET Core)

TCP transport supports connection pooling and multiplexing requests within one connection, infrastructure itself allows multiple "services" to be hosted inside one host. You may define your own handler factory or "routing" mechanism. Serializer (JSON.NET is used by default) is also easy to replace.

### Protocol definition

Put an interface into your shared library and reference it from both client and server apps.

```cs
public interface IService
{
    Task<string> Foo(int bar);
}
```

### Server

Implement the declared interface in your server app.

```cs
public class Service : IService
{
    public Task<string> Foo(int bar)
    {
        return Task.FromResult(bar.ToString());
    }
}
```

Start your server using either TCP or HTTP protocol.

```cs
// TCP
public void StartServer()
{
    var router = new DefaultTargetSelector();
    router.Register<IService, Service>();
    var host = new TcpHost(new Engine().CreateRequestHandler(router));
    host.StartListening(new IPEndPoint(IPAddress.Loopback, 9000));
}
    
// ASP.NET Core (HTTP)
public void Configure(IApplicationBuilder app, IHostingEnvironment env)
{
    var router = new DefaultTargetSelector();
    router.Register<IService, Service>();
    app.UseCoreRPC("/rpc", new Engine().CreateRequestHandler(router));
}
```

### Client

Use the protocol your server uses and call remote procedures!

```cs
// TCP
var transport = new TcpClientTransport(IPAddress.Parse("127.0.0.1");
var proxy = new Engine().CreateProxy<IService>(transport, 9000));

// HTTP 
var transport = new HttpClientTransport("http://example.com/rpc");
var proxy = new Engine().CreateProxy<IService>(transport);
var res = await proxy.Foo(1);
```

### Assembly scanning

CoreRPC.AspNetCore package supports automatic RPC registration. All you need is to mark your class with `RegisterRpc` attribute and it will be registered automatically once you add a call to `.UseCoreRpc` on your app builder:

```cs
// Implement the shared interface.
[RegisterRpc(typeof(IService))]
public class Service : IService
{
    public Task<string> Foo(int bar)
    {
        return Task.FromResult(bar.ToString());
    }
}

// It will get registered automatically.
public void Configure(IApplicationBuilder app, IHostingEnvironment env)
{
    app.UseCoreRpc("/rpc");
}
```

You can override the built-in assembly scanning engine if necessary:

```cs
// This may be useful when writing integrational tests, etc.
app.UseCoreRpc("/rpc", config => config.RpcTypeResolver = () =>
{
    // Skip abstract classes and types not marked with [RegisterRpc] (default behavior).
    var assembly = Assembly.GetAssembly(typeof(Startup));
    return assembly.DefinedTypes
        .Where(type => !type.IsAbstract || type.IsInterface && 
               type.GetCustomAttribute<RegisterRpcAttribute>() != null);
});
```
