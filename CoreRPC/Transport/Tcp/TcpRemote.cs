using System.Net;

namespace CoreRPC.Transport.Tcp
{
    internal class TcpRemote
    {
        public IPAddress Host;
        public int Port;

        public TcpRemote (IPAddress host, int port)
        {
            Host = host;
            Port = port;
        }
    }
}