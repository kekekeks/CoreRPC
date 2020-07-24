using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using CoreRPC.Utility;
using Microsoft.IO;

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
                var pipe = new NamedPipeServerStream(
                    pipeName, PipeDirection.InOut, -1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);
                try
                {
                    await pipe.WaitForConnectionAsync(token);
                }
                catch
                {
                    pipe.Dispose();
                    continue;
                }
                ProcessRequest(pipe, token);
            }
        });

        private void ProcessRequest(NamedPipeServerStream pipe, CancellationToken token) => Task.Run(async () =>
        {
            using (pipe)
            {
                var requestLengthBytes = await pipe.ReadExactlyAsync(4);
                var requestLength = BitConverter.ToInt32(requestLengthBytes, 0);
                Stream response = null;
                using (var request = new RecyclableMemoryStream(StreamPool.Shared))
                {
                    await pipe.ReadExactlyAsync(request, requestLength);
                    request.Position = 0;

                    await _engine.HandleRequest(new Request(request, async response =>
                    {
                        var responseLengthBytes = BitConverter.GetBytes(response.Length);
                        await pipe.WriteAsync(responseLengthBytes, 0, 4, token);
                        await response.CopyToAsync(pipe, 81920, token);
                        await pipe.FlushAsync(token);
                    }));
                }

                
                
            }
        });

        private sealed class Request : IRequest
        {
            private readonly Func<Stream, Task> _respond;

            public Request(Stream data, Func<Stream, Task> respond)
            {
                Data = data;
                _respond = respond;
            }

            public Stream Data { get; }

            public object Context { get; } = null;

            public Task RespondAsync(Stream data) => _respond(data);
        }
    }
}
