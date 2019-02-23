using System;
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
                var pipe = new NamedPipeServerStream(
                    pipeName, PipeDirection.InOut, 1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);
                try
                {
                    await pipe.WaitForConnectionAsync(token);
                    ProcessRequest(pipe, token);
                }
                catch (OperationCanceledException)
                {
                    pipe.Dispose();
                }
            }
        });

        private void ProcessRequest(NamedPipeServerStream pipe, CancellationToken token) => Task.Run(async () =>
        {
            using (pipe)
            {
                var requestLengthBytes = await pipe.ReadExactlyAsync(4);
                var requestLength = BitConverter.ToInt32(requestLengthBytes, 0);
                var request = await pipe.ReadExactlyAsync(requestLength);

                var message = new byte[0];
                await _engine.HandleRequest(new Request(request, bytes => message = bytes));

                var responseLengthBytes = BitConverter.GetBytes(message.Length);
                await pipe.WriteAsync(responseLengthBytes, 0, 4, token);
                await pipe.WriteAsync(message, 0, message.Length, token);
                await pipe.FlushAsync(token);
            }
        });

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
