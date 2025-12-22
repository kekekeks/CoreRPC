using System.Reflection;
using CoreRPC.AspNetCore;
using CoreRpcUeTestApp;
using Newtonsoft.Json.Serialization;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

var builder = new WebHostBuilder()
    .UseKestrel()
    .Configure(b =>
    {
        b.UseCoreRpc("/rpc");
        if (args is ["--GenerateApi", _])
        {
            AspNetCoreRpcUnrealEngineCodeGenerator.GenerateCode(args[1],b.ApplicationServices.GetRequiredService<IHostingEnvironment>());
        }
    })
    .ConfigureServices(s =>
    {

    });
var host = builder.Build();
host.Run();