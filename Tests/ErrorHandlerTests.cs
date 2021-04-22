using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CoreRPC;
using CoreRPC.AspNetCore;
using CoreRPC.Routing;
using CoreRPC.Transport;
using CoreRPC.Transport.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tests
{
    public class ErrorHandlerTests
    {
        private const string PublicErrorMessage = "Internal server error!";
        private const string PrivateErrorMessage = "Hello, world!";

        public interface IThrowExceptions
        {
            Task ThrowException();
        }

        [RegisterRpc(typeof(IThrowExceptions))]
        public class ThrowsIoExceptions : IThrowExceptions
        {
            public Task ThrowException() => throw new IOException(PrivateErrorMessage);
        }

        private class ThrowsIoExceptionsSelector : ITargetSelector
        {
            public object GetTarget(string target, object callContext) => new ThrowsIoExceptions();
        }

        public class CustomErrorHandler : IRequestErrorHandler
        {
            private readonly string _response;

            public CustomErrorHandler(string response) => _response = response;

            public List<Exception> Errors { get; } = new List<Exception>();

            public string HandleError(Exception exception)
            {
                Errors.Add(exception);
                return _response;
            }
        }

        [Fact]
        public async Task CoreRpc_Should_Forward_Exceptions_To_Request_Error_Handler()
        {
            var engine = new Engine();
            var handler = new CustomErrorHandler(PublicErrorMessage);
            var proxy = engine.CreateProxy<IThrowExceptions>(
                new InternalThreadPoolTransport(
                    engine.CreateRequestHandler(
                        new ThrowsIoExceptionsSelector(),
                        handler)));

            var error = await Assert.ThrowsAnyAsync<Exception>(() => proxy.ThrowException());

            Assert.NotEmpty(handler.Errors);
            Assert.NotNull(handler.Errors[0].InnerException?.Message);
            Assert.Equal(PrivateErrorMessage, handler.Errors[0].InnerException?.Message);
            Assert.Equal(PublicErrorMessage, error.Message);
        }

        [Fact]
        public async Task CoreRpc_Should_Respond_With_Exception_Details_If_There_Is_No_Error_Handler()
        {
            var engine = new Engine();
            var proxy = engine.CreateProxy<IThrowExceptions>(
                new InternalThreadPoolTransport(
                    engine.CreateRequestHandler(
                        new ThrowsIoExceptionsSelector())));

            var error = await Assert.ThrowsAnyAsync<Exception>(() => proxy.ThrowException());
            Assert.Contains(PrivateErrorMessage, error.Message);
        }

        public class Startup
        {
            public void ConfigureServices(IServiceCollection services) =>
                services.AddSingleton(
                    new CustomErrorHandler(
                        PublicErrorMessage));

            public void Configure(IApplicationBuilder app) =>
                app.UseCoreRpc("/rpc", options =>
                {
                    // Here we specify the custom error handler.
                    options.ErrorHandler = app.ApplicationServices.GetService<CustomErrorHandler>();
                    options.RpcTypeResolver = () => new[] {typeof(ThrowsIoExceptions)};
                });
        }

        [Fact]
        public async Task UseCoreRpc_Should_Support_Custom_Error_Handlers()
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseFreePort()
                .UseStartup<Startup>()
                .Build();

            await host.StartAsync();
            var addresses = host.ServerFeatures.Get<IServerAddressesFeature>();
            var address = addresses.Addresses.First().TrimEnd('/') + "/rpc";
            var handler = host.Services.GetRequiredService<CustomErrorHandler>();

            var transport = new HttpClientTransport(address);
            var proxy = new Engine().CreateProxy<IThrowExceptions>(transport);
            var error = await Assert.ThrowsAnyAsync<Exception>(() => proxy.ThrowException());

            Assert.NotEmpty(handler.Errors);
            Assert.NotNull(handler.Errors[0].InnerException?.Message);
            Assert.Equal(PrivateErrorMessage, handler.Errors[0].InnerException?.Message);
            Assert.Equal(PublicErrorMessage, error.Message);
        }
    }
}