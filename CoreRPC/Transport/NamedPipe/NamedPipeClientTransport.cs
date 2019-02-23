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
            var writer = new BinaryWriter(pipe);
            var reader = new BinaryReader(pipe);

            await pipe.ConnectAsync();
            writer.Write(message.Length);
            writer.Write(message);
            writer.Flush();

            var length = reader.ReadInt32();
            var response = reader.ReadBytes(length);
            return response;
        }
    }
}
