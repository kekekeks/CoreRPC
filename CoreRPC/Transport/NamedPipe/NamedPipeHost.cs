using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
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
                using (var reader = new StreamReader(_pipe))
                using (var writer = new StreamWriter(_pipe))
                {
                    await _pipe.WaitForConnectionAsync();
                    var message = await reader.ReadLineAsync();
                    var messageBytes = Encoding.UTF8.GetBytes(message);

                    var responseBytes = new byte[0];
                    await _engine.HandleRequest(new Request(messageBytes, bytes => responseBytes = bytes));

                    var responseString = Encoding.UTF8.GetString(responseBytes);
                    await writer.WriteLineAsync(responseString);
                    await writer.FlushAsync();
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
