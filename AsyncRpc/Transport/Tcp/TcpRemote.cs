namespace AsyncRpc.Transport.Tcp
{
	internal class TcpRemote
	{
		public string Host;
		public int Port;

		public TcpRemote (string host, int port)
		{
			Host = host;
			Port = port;
		}
	}
}