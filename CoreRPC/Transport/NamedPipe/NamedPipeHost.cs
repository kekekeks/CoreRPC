using System;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace CoreRPC.Transport.NamedPipe
{
    public sealed class NamedPipeHost : IDisposable
    {
        private readonly IRequestHandler _engine;
        private NamedPipeServerStream _pipe;
        private bool _isDisposed;

        public NamedPipeHost(IRequestHandler engine) => _engine = engine;

        public void StartListening(string pipeName) => Task.Run(async () =>
        {
            while (!_isDisposed)
            {
                using (_pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut))
                {
                    await _pipe.WaitForConnectionAsync();

                    var requestLengthBytes = await _pipe.ReadExactlyAsync(4);
                    var requestLength = BitConverter.ToInt32(requestLengthBytes, 0);
                    var request = await _pipe.ReadExactlyAsync(requestLength);

                    var message = new byte[0];
                    await _engine.HandleRequest(new Request(request, bytes => message = bytes));

                    var responseLengthBytes = BitConverter.GetBytes(message.Length);
                    await _pipe.WriteAsync(responseLengthBytes, 0, 4);
                    await _pipe.WriteAsync(message, 0, message.Length);
                    await _pipe.FlushAsync();
                }
            }
        });

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            _pipe?.Dispose();
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
