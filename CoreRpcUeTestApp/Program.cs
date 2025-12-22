using System.Reflection;
using CoreRPC.AspNetCore;
using CoreRpcUeTestApp;
using Newtonsoft.Json.Serialization;

var builder = new WebHostBuilder()
    .UseKestrel()
    .Configure(b =>
    {
        b.UseCoreRpc("/rpc");
        if (args is ["--GenerateApi", _])
        {
            UeRpcCodeGen.GenerateCode(args[1], typeof(Program).Assembly);
        }
    })
    .ConfigureServices(s =>
    {

    });
var host = builder.Build();
host.Run();