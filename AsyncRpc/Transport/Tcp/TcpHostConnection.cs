using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncRpc.Transport.Tcp
{
	internal class TcpHostConnection : TcpConnection
	{
		private readonly TcpHost _tcpHost;
		private readonly NetworkStream _stream;
		private readonly IRequestHandler _engine;
		
		private TcpHostConnection(TcpHost tcpHost, NetworkStream stream, IRequestHandler engine):base(stream)
		{
			if (tcpHost == null) throw new ArgumentNullException("tcpHost");
			if (stream == null) throw new ArgumentNullException("stream");
			if (engine == null) throw new ArgumentNullException("engine");
			_tcpHost = tcpHost;
			_stream = stream;
			_engine = engine;
		}


		private class Request : IRequest
		{
			private readonly TcpHostConnection _conn;
			private readonly ulong _requestId;

			public Request(TcpHostConnection conn, byte[] rawData)
			{
				_conn = conn;
				_requestId = BitConverter.ToUInt64(rawData, 0);
				Data = new byte[rawData.Length - 8];
				Array.Copy(rawData, 8, Data, 0, Data.Length);
			}

			public byte[] Data { get; private set; }

			public Task RespondAsync(byte[] data)
			{
				var packet = new byte[data.Length + 12];
				BitConverter.GetBytes(data.Length + 8).CopyTo(packet, 0);
				BitConverter.GetBytes(_requestId).CopyTo(packet, 4);
				data.CopyTo(packet, 12);
				return _conn.SendBytesAsync(packet);
			}
		}

		private async void StartReading()
		{
			try
			{
				while (true)
				{
					var size = BitConverter.ToInt32(await _stream.ReadExactlyAsync(4), 0);
					var req = new Request(this, await _stream.ReadExactlyAsync(size));
					Task.Run(() => _engine.HandleRequest(req)).GetAwaiter();
				}
			}
			catch(Exception e)
			{
				_stream.Dispose();
				_tcpHost.FireNetworkError(e);
			}
		}

		public static void HandleNew(TcpHost tcpHost, TcpClient cl, IRequestHandler engine)
		{
			new TcpHostConnection (tcpHost, cl.GetStream (), engine).StartReading ();
		}
	}
}
