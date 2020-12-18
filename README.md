[![NuGet Stats](https://img.shields.io/nuget/v/CoreRpc.svg)](https://www.nuget.org/packages/CoreRpc) [![Downloads](https://img.shields.io/nuget/dt/corerpc.svg)](https://www.nuget.org/packages/corerpc) ![License](https://img.shields.io/github/license/kekekeks/corerpc.svg)

# CoreRPC

Extensible library for WCF-like RPC targeting netstandard1.3 (compatible with .NET, .NET Framework, Mono and .NET Core). TCP transport supports connection pooling and multiplexing requests within one connection, infrastructure itself allows multiple "services" to be hosted inside one host. You may define your own handler factory or "routing" mechanism. Serializer (JSON.NET is used by default) is also easy to replace.

## NuGet Packages

Install the following package into your projects.

| Target             | CoreRPC Package                 | NuGet                |
| ------------------ | ------------------------------- | -------------------- |
| Standalone         | [CoreRPC][CorePkg]              | [![CoreBadge]][CorePkg] |
| ASP .NET Core      | [CoreRPC.AspNetCore][AspPkg]    | [![AspBadge]][AspPkg] |

[CoreBadge]: https://img.shields.io/nuget/v/CoreRpc.svg
[CorePkg]: https://www.nuget.org/packages/CoreRpc
[AspBadge]: https://img.shields.io/nuget/v/CoreRpc.AspNetCore.svg
[AspPkg]: https://www.nuget.org/packages/CoreRpc.AspNetCore

### Protocol Definition

Put an interface into your shared library and reference it from both client and server apps:

```cs
public interface IService
{
    Task<string> Foo(int bar);
}
```

### Server App

Implement the declared interface:

```cs
public class Service : IService
{
    public Task<string> Foo(int bar)
    {
        return Task.FromResult(bar.ToString());
    }
}
```

Configure the router and start your server using either TCP, HTTP, or Named Pipes:

```cs
// Configure the router.
var router = new DefaultTargetSelector();
router.Register<IService, Service>();
var engine = new Engine().CreateRequestHandler(router);

// Use Named Pipes.
new NamedPipeHost(engine).StartListening("AwesomePipeName");

// Or, use HTTP (ASP.NET Core).
public void Configure(IApplicationBuilder app, IHostingEnvironment env) {
    app.UseCoreRPC("/rpc", engine);
}
```

### Client App

Use the protocol your server uses, and call remote procedures!

```cs
// Use HTTP. 
var transport = new HttpClientTransport("http://example.com/rpc");

// Or, use Named Pipes.
var transport = new NamedPipeClientTransport("AwesomePipeName");

// Crete the proxy and call remote procedures!
var proxy = new Engine().CreateProxy<IService>(transport);
var res = await proxy.Foo(1);
```

## Assembly Scanning

`CoreRPC.AspNetCore` package supports automatic RPC registration. All you need is to mark your classes with the `RegisterRpc` attribute. CoreRPC will discover and register classes marked with that attribute automatically once you add a call to `.UseCoreRpc()` to your app builder:

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
    // CoreRPC will register the class maked with RegisterRpc automatically.
    app.UseCoreRpc("/rpc");
}
```

You can override the built-in assembly scanning engine and a few other options if necessary:

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

## RPC Interception

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

## TypeScript API Generation

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

## Usage Examples

See [tests](https://github.com/kekekeks/CoreRPC/tree/master/Tests) for more examples.
