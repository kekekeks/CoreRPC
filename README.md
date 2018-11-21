# CoreRPC

Extensible library for WCF-like RPC targeting netstandard1.3 (compatible with .NET, Mono and .NET Core)

TCP transport supports connection pooling and multiplexing requests within one connection, infrastructure itself allows multiple "services" to be hosted inside one host. You may define your own handler factory or "routing" mechanism. Serializer (JSON.NET is used by default) is also easy to replace.

### Protocol Definition

Put an interface into your shared library and reference it from both client and server apps.

```cs
public interface IService
{
    Task<string> Foo(int bar);
}
```

### Server

Implement the declared interface.

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

### Assembly Scanning

`CoreRPC.AspNetCore` package supports automatic RPC registration. All you need is marking classes with `RegisterRpc` attribute. Classes marked with that attribute will get registered automatically once you add a call to `.UseCoreRpc()` to your app builder:

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

public void Configure(IApplicationBuilder app, IHostingEnvironment env)
{
    // CoreRPC will register the class maked with RegisterRpc 
    // attribute automagically.
    app.UseCoreRpc("/rpc");
}
```

You can override the built-in assembly scanning engine and some other options if necessary:

```cs
// This may be useful when writing integrational tests, etc.
app.UseCoreRpc("/rpc", config => 
{
    // By default, CoreRPC uses camel case resolver, but you can override that.
    config.JsonSerializer.ContractResolver = new DefaultContractResolver();
    config.RpcTypeResolver = () =>
    {
        // Skip abstract classes and types not marked with [RegisterRpc] (default behavior).
        var assembly = Assembly.GetAssembly(typeof(Startup));
        return assembly.DefinedTypes
            .Where(type => !type.IsAbstract || type.IsInterface && 
                   type.GetCustomAttribute<RegisterRpcAttribute>() != null);
    };
    
    // You can also add a custom RPC method call interceptor.
    config.Interceptors.Add(new MyMethodCallInterceptor());
});
```

### RPC Interception

If you need to intercept RPCs, implement the `IMethodCallInterceptor` interface and register it:

```cs
// The simplest interceptor that apparently does nothing.
public class Interceptor : IMethodCallInterceptor
{
    public Task<object> Intercept(MethodCall call, object context, Func<Task<object>> invoke)
    {
        // Add your custom logic here.
        return invoke();
    }
}

// Register the method call interceptor.
app.UseCoreRpc("/rpc", config => config.Interceptors.Add(new Interceptor()));
```

### TypeScript API Generation

CoreRPC provides you an ability to generate TypeScript code for your server-side API.

```cs
// Store the generated TS code somewhere, e.g. save it to 'api.ts' file.
// Putting such C# code into your Startup.cs file might be a good idea.
string generatedCode = AspNetCoreRpcTypescriptGenerator.GenerateCode(types, config =>
{
    config.ApiFieldNamingPolicy = type => type.Replace("Rpc", string.Empty);
    config.DtoFieldNamingPolicy = TypescriptGenerationOptions.ToCamelCase;
});
```

### Usage Examples

See [tests](https://github.com/kekekeks/CoreRPC/tree/master/Tests) for more examples.
