using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using CoreRPC;
using CoreRPC.AspNetCore;
using CoreRPC.Transferable;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tests
{
    public interface IMyRpc1
    {
        bool Foo(int bar);
    }
    
    [RegisterRpc]
    public interface IMyRpc2
    {
        string Bar(int foo);
    }

    [RegisterRpc(typeof(IMyRpc1))]
    public class MyRpc1 : IMyRpc1
    {
        public bool Foo(int bar)
        {
            return bar > 2;
        }
    }

    public class MyRpc2 : IMyRpc2
    {
        public string Bar(int foo)
        {
            return foo.ToString();
        }
    }

    [RegisterRpc]
    public class MyRpc3
    {
        public Task<string> Foo() => Task.Delay(10).ContinueWith(_ => "test");
        public string Intercepted(int foo, int bar) => throw new NotSupportedException();
    }

    [RegisterRpc]
    public class ContextAwareRpc : IHttpContextAwareRpc
    {
        private HttpContext _context;
        Task<object> IHttpContextAwareRpc.OnExecuteRpcCall(HttpContext context, Func<Task<object>> action)
        {
            _context = context;
            return action();
        }

        public string GetHeader() => _context.Request.Headers["X-Test"].ToString();
    }

    [RegisterRpc("MySuperName")]
    public class NamedRpc
    {
        public int Echo(int num) => num;
    }

    public class MyGenericDto<T1, T2>
    {
        public T1 Prop1 { get; set; }
        public T2 Prop2 { get; set; }
    }
    
    [RegisterRpc]
    public class GenericRpcParams
    {
        public Dictionary<string, int> Do(MyGenericDto<int, string> dto) => new Dictionary<string, int> {[dto.Prop2] = dto.Prop1};
    }

    public class MyStaticFieldsDto
    {
        public string MyStaticFieldsDtoRegularProperty { get; set; } = "Foo";
        public static string StaticPropertyThisTextShouldNotExistInGeneratedFile { get; set; } = "I'm a static.";
    }

    [RegisterRpc]
    public class StaticFields
    {
        public MyStaticFieldsDto Do() => new MyStaticFieldsDto();
    }

    public class MyTsIgnoreDto
    {
        public string MyTsIgnoreDtoRegularProperty { get; set; } = "Foo";

        [TsIgnore]
        public string ThisTextShouldNotExistInGeneratedFile { get; set; } = "I'm ignored.";

        [TsIgnore]
        public string ComputedThisTextShouldNotExistInGeneratedFile => "I'm computed.";
    }

    [RegisterRpc]
    public class TsIgnoreProperties
    {
        public MyTsIgnoreDto Do() => new MyTsIgnoreDto();
    }

    class RpcStartup
    {
        public static string JsDir;
        public RpcStartup(IHostingEnvironment env)
        {
            var code =
                "import {default as fetch, RequestInit, Response} from \"node-fetch\";\n" +
                AspNetCoreRpcTypescriptGenerator.GenerateCode(env);
            File.WriteAllText(Path.Combine(JsDir, "api.ts"), code);
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IMyRpc2, MyRpc2>();
        }

        class Interceptor : IMethodCallInterceptor
        {
            public Task<object> Intercept(MethodCall call, object context, Func<Task<object>> invoke)
            {
                if (call.Method.Name == "Intercepted")
                {
                    var hdr = ((HttpContext) context).Request.Headers["X-Test"].ToString();
                    return Task.FromResult((object) (hdr + call.Arguments[0] + call.Arguments[1]));
                }

                return invoke();
            }
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseCoreRpc("/rpc", cfg => cfg.Interceptors.Add(new Interceptor()));
        }
    }
#if !LEGACY_NET
    public class TypescriptAspNetCoreTests
    {
        [Fact]
        public void RunJsTests()
        {
            var dir = Path.GetDirectoryName(typeof(TypescriptAspNetCoreTests).Assembly.GetModules()[0].FullyQualifiedName);
            string jsDir;
            while (!Directory.Exists(jsDir = Path.Combine(dir, "jsapp")))
                dir = Path.GetFullPath(Path.Combine(dir, ".."));
            RpcStartup.JsDir = jsDir;

            var builder = new WebHostBuilder()
                .UseKestrel()
                .UseFreePort()
                .UseStartup<RpcStartup>();
            var host = builder.Build();
            host.Start();
            var address = host.ServerFeatures.Get<IServerAddressesFeature>();
            var addr = address.Addresses.First() + "/rpc";

            var proc = Process.Start(new ProcessStartInfo("npm", "start --silent")
            {
                WorkingDirectory = jsDir,
                Environment =
                {
                    ["APIADDR"] = addr
                },
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });
            var outputTask = proc.StandardOutput.ReadToEndAsync();
            var errorTask = proc.StandardError.ReadToEndAsync();
            proc.WaitForExit();
            var output = outputTask.Result;
            if (output.Trim() != "OK")
                throw new Exception(errorTask.Result);

            var apiTs = Path.Combine(jsDir, "api.ts");
            var generatedStuff = File.ReadAllText(apiTs);
            Assert.Contains(nameof(MyStaticFieldsDto.MyStaticFieldsDtoRegularProperty), generatedStuff);
            Assert.Contains(nameof(MyTsIgnoreDto.MyTsIgnoreDtoRegularProperty), generatedStuff);
            Assert.DoesNotContain("ThisTextShouldNotExistInGeneratedFile", generatedStuff);
        }
    }
#endif
}