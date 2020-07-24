using System;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using CoreRPC.Utility;
using Microsoft.IO;

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

        
        public async Task<Stream> SendMessageAsync(Stream message)
        {
            using (var pipe = new NamedPipeClientStream(_serverName, _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                await pipe.ConnectAsync();

                var requestLengthBytes = BitConverter.GetBytes(message.Length);
                await pipe.WriteAsync(requestLengthBytes, 0, 4);
                await message.CopyToAsync(pipe);
                await pipe.FlushAsync();

                var responseLengthBytes = await pipe.ReadExactlyAsync(4);
                var responseLength = BitConverter.ToInt32(responseLengthBytes, 0);

                bool success = false;
                var ms = new RecyclableMemoryStream(StreamPool.Shared);
                try
                {
                    await pipe.ReadExactlyAsync(ms, responseLength);
                    ms.Position = 0;
                    success = true;
                }
                finally
                {
                    if(!success)
                        ms.Dispose();
                }

                return ms;
            }
        }
    }
}
