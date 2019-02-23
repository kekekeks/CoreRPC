using System;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace CoreRPC.Transport.NamedPipe
{
    public sealed class NamedPipeClientTransport : IClientTransport
    {
        private readonly string _serverName;
        private readonly string _pipeName;

        public NamedPipeClientTransport(string pipeName, string serverName = ".")
        {
            _serverName = serverName;
            _pipeName = pipeName;
        }

        public async Task<byte[]> SendMessageAsync(byte[] message)
        {
            using (var pipe = new NamedPipeClientStream(_serverName, _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                await pipe.ConnectAsync();

                var requestLengthBytes = BitConverter.GetBytes(message.Length);
                await pipe.WriteAsync(requestLengthBytes, 0, 4);
                await pipe.WriteAsync(message, 0, message.Length);
                await pipe.FlushAsync();

                var responseLengthBytes = await pipe.ReadExactlyAsync(4);
                var responseLength = BitConverter.ToInt32(responseLengthBytes, 0);
                var response = await pipe.ReadExactlyAsync(responseLength);
                return response;
            }
        }
    }
}
