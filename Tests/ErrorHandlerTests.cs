using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CoreRPC;
using CoreRPC.Routing;
using CoreRPC.Transport;
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

        public class ThrowsIoExceptions : IThrowExceptions
        {
            public Task ThrowException() => throw new IOException(PrivateErrorMessage);
        }

        private class ThrowsIoExceptionsSelector : ITargetSelector
        {
            public object GetTarget(string target, object callContext) => new ThrowsIoExceptions();
        }

        public class SampleRequestErrorHandler : IRequestErrorHandler
        {
            private readonly string _response;

            public SampleRequestErrorHandler(string response) => _response = response;

            public List<Exception> Errors { get; } = new List<Exception>();

            public string HandleError(Exception exception)
            {
                Errors.Add(exception);
                return _response;
            }
        }

        [Fact]
        public async Task CoreRPC_Should_Forward_Exceptions_To_Request_Error_Handler_If_Available()
        {
            var engine = new Engine();
            var handler = new SampleRequestErrorHandler(PublicErrorMessage);
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
    }
}