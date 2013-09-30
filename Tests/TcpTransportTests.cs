using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using AsyncRpc.Transport;
using Xunit;

namespace Tests
{
	public class TcpTransportTests
	{
		[Fact]
		public void CheckConnectivity()
		{
			var port = GetFreePort();
			var server = new AsyncRpc.Transport.Tcp.TcpHost(new Handler());
			server.StartListening(new IPEndPoint(IPAddress.Loopback, port));
			var client = new AsyncRpc.Transport.Tcp.TcpClientTransport("127.0.0.1", port);

			var message = new byte[] {1, 2, 3, 4, 5};
			var task = client.SendMessageAsync(message);
			task.Wait();

			Assert.True(task.Result.SequenceEqual(message));
		}

		static int GetFreePort()
		{
			var listener = new TcpListener(IPAddress.Loopback, 0);
			listener.Start();
			var port = ((IPEndPoint) listener.LocalEndpoint).Port;
			listener.Stop();
			return port;
		}

		public class Handler : IRequestHandler
		{
			public void HandleRequest(IRequest req)
			{
				req.RespondAsync(req.Data);
			}
		}
	}

	
}
