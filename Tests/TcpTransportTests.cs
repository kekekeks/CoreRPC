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
			
			using (var server = new AsyncRpc.Transport.Tcp.TcpHost(new Handler()))
			{
				server.StartListening(new IPEndPoint(IPAddress.Loopback, port));
				var client = new AsyncRpc.Transport.Tcp.TcpClientTransport(IPAddress.Parse("127.0.0.1"), port);

				var message = new byte[] {1, 2, 3, 4, 5};
				var data = client.SendMessageAsync(message).Result;
				Assert.True(data.SequenceEqual(message));
			}
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
			public Task HandleRequest(IRequest req)
			{
				return req.RespondAsync(req.Data);
			}
		}
	}

	
}
