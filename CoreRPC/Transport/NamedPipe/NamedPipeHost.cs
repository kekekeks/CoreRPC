using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace CoreRPC.Transport.NamedPipe
{
    public sealed class NamedPipeHost
    {
        private readonly IRequestHandler _engine;

        public NamedPipeHost(IRequestHandler engine) => _engine = engine;

        public void StartListening(string pipeName, CancellationToken token = default(CancellationToken)) => Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                using (var pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut))
                {
                    await pipe.WaitForConnectionAsync(token);
                    var request = await Task.Run(async () => await ReadResponse(pipe), CancellationToken.None);

                    var message = new byte[0];
                    await _engine.HandleRequest(new Request(request, bytes => message = bytes));

                    var responseLengthBytes = BitConverter.GetBytes(message.Length);
                    await pipe.WriteAsync(responseLengthBytes, 0, 4, token);
                    await pipe.WriteAsync(message, 0, message.Length, token);
                    await pipe.FlushAsync(token);
                }
            }
        });

        private static async Task<byte[]> ReadResponse(Stream stream)
        {
            var lengthBytes = await stream.ReadExactlyAsync(4);
            var length = BitConverter.ToInt32(lengthBytes, 0);
            var message = await stream.ReadExactlyAsync(length);
            return message;
        }

        private sealed class Request : IRequest
        {
            private readonly Action<byte[]> _respond;

            public Request(byte[] data, Action<byte[]> respond)
            {
                Data = data;
                _respond = respond;
            }

            public byte[] Data { get; }

            public object Context { get; } = null;

            public Task RespondAsync(byte[] data)
            {
                _respond(data);
                return Task.CompletedTask;
            }
        }
    }
}
