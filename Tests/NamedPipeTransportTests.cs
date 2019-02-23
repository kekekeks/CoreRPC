using CoreRPC.Transport.NamedPipe;
using System.Linq;
using System.Threading.Tasks;
using CoreRPC.Transport;
using Xunit;

namespace Tests
{
    public class NamedPipeTransportTests
    {
        [Fact]
        public async Task CheckConnectivity()
        {
            const string pipe = "NamedPipeTransportTests";
            using (var server = new NamedPipeHost(new Handler()))
            {
                server.StartListening(pipe);
                var message = new byte[] { 1, 2, 3, 4, 5 };
                var client = new NamedPipeClientTransport(pipe);

                var data = await client.SendMessageAsync(message);
                Assert.True(data.SequenceEqual(message));
            }
        }

        public class Handler : IRequestHandler
        {
            public Task HandleRequest(IRequest req) => req.RespondAsync(req.Data);
        }
    }
}
