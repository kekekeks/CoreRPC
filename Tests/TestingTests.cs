using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoreRPC.AspNetCore;
using CoreRPC.Testing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tests
{
    public class TestingTests
    {
        public class InjectableService
        {
            public int Sum(int a, int b) => a + b;

            public int Multiply(int a, int b) => a * b;
        }

        [RegisterRpc]
        public class AnonComputeRpc
        {
            private readonly InjectableService _service;

            public AnonComputeRpc(InjectableService service) => _service = service;

            public int Sum(int a, int b) => _service.Sum(a, b);

            public int Multiply(int a, int b) => _service.Multiply(a, b);
        }

        [RegisterRpc]
        public class AnonGreeterRpc
        {
            public string Greet(string name) => $"Hello, {name}!";
        }

        [RegisterRpc]
        public class SecureComputeRpc : IHttpContextAwareRpc
        {
            private const string SecureToken = "foobar";
            private readonly InjectableService _service;

            public SecureComputeRpc(InjectableService service) => _service = service;

            public int Sum(int a, int b) => _service.Sum(a, b);

            public int Multiply(int a, int b) => _service.Multiply(a, b);

            public Task<object> OnExecuteRpcCall(HttpContext context, Func<Task<object>> action)
            {
                var header = context.Request.Headers["X-Auth"].FirstOrDefault();
                if (header != null && header == SecureToken)
                    return action();
                context.Response.StatusCode = 401;
                return Task.FromResult((object) new { Error = "Not authorized!" });
            }
        }

        public class Startup
        {
            public void ConfigureServices(IServiceCollection services) => services.AddSingleton<InjectableService>();

            public void Configure(IApplicationBuilder app) =>
                app.UseCoreRpc("/rpc", options => options.RpcTypeResolver = () => new[]
                {
                    typeof(SecureComputeRpc),
                    typeof(AnonComputeRpc),
                    typeof(AnonGreeterRpc),
                });
        }

        // This is a sample helper class used for writing unit tests.
        public sealed class AnonRpcList : RpcListBase
        {
            public AnonRpcList(string uri) : base(uri) { }

            public IRpcExec<AnonComputeRpc> Compute => Get<AnonComputeRpc>();
            public IRpcExec<AnonGreeterRpc> Greeter => Get<AnonGreeterRpc>();
        }

        // This is a sample helper class used for writing unit tests for authorized RPCs.
        public sealed class SecureRpcList : RpcListBase
        {
            public SecureRpcList(string uri) : base(uri, new Dictionary<string, string> {["X-Auth"] = "foobar"}) { }

            public IRpcExec<SecureComputeRpc> Compute => Get<SecureComputeRpc>();
        }

        [Theory]
        [InlineData(1, 1, 2, 1)]
        [InlineData(2, 2, 4, 4)]
        [InlineData(3, 2, 5, 6)]
        public async Task ShouldCallRemoteProceduresWithoutInterfaceDefinition(int a, int b, int sum, int product)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseFreePort()
                .UseStartup<Startup>()
                .Build();

            await host.StartAsync();
            var addresses = host.ServerFeatures.Get<IServerAddressesFeature>();
            var address = addresses.Addresses.First().TrimEnd('/') + "/rpc";

            var anon = new AnonRpcList(address);
            Assert.Equal("Hello, John!", anon.Greeter.Call(api => api.Greet("John")));
            Assert.Equal(sum, anon.Compute.Call(api => api.Sum(a, b)));
            Assert.Equal(product, anon.Compute.Call(api => api.Multiply(a, b)));

            var secure = new SecureRpcList(address);
            Assert.Equal(sum, secure.Compute.Call(api => api.Sum(a, b)));
            Assert.Equal(product, secure.Compute.Call(api => api.Multiply(a, b)));
        }
    }
}