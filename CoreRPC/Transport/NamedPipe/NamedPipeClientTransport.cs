using System.IO;
using System.IO.Pipes;
using System.Text;
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
            var pipe = new NamedPipeClientStream(_serverName, _pipeName, PipeDirection.InOut);
            var messageString = Encoding.UTF8.GetString(message);
            var writer = new StreamWriter(pipe);
            var reader = new StreamReader(pipe);

            await pipe.ConnectAsync();
            await writer.WriteLineAsync(messageString);
            await writer.FlushAsync();

            var response = await reader.ReadLineAsync();
            var responseBytes = Encoding.UTF8.GetBytes(response);
            return responseBytes;
        }
    }
}
