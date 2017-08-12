using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace CoreRPC.Transport.Tcp
{
	internal class TcpClientConnection : TcpConnection, IDisposable
	{
		private readonly NetworkStream _stream;
		private readonly Dictionary<long, TaskCompletionSource<byte[]>> _pendingRequests = new Dictionary<long, TaskCompletionSource<byte[]>> ();
		private long _nextRequestId = 1;
		private readonly object _syncRoot = new object();
		private Exception _ioException;

		public TcpClientConnection(NetworkStream stream)
			: base(stream)
		{
			_stream = stream;
			ReadNextResponse();
		}

		public static async Task<TcpClientConnection> ConnectAsync(IPAddress addr, int port)
		{			
			var cl = new TcpClient();
			await cl.ConnectAsync(new[] { addr }, port);
			return new TcpClientConnection(cl.GetStream());
		}


		public void Dispose()
		{
			_stream.Dispose();
		}

		public async Task<byte[]> SendMessageAsync(byte[] data)
		{
			var rid = Interlocked.Increment(ref _nextRequestId);
			var packet = new byte[data.Length + 12];
			BitConverter.GetBytes(data.Length + 8).CopyTo(packet, 0);
			BitConverter.GetBytes(rid).CopyTo(packet, 4);
			data.CopyTo(packet, 12);
			var tcs = new TaskCompletionSource<byte[]>();
			lock (_syncRoot)
			{
				if (_ioException != null)
					throw new AggregateException(_ioException);
				_pendingRequests[rid] = tcs;	
			}
			try
			{
				await SendBytesAsync(packet).ConfigureAwait(false);
			}
			catch(Exception e)
			{
				lock (_syncRoot)
					_pendingRequests.Remove(rid);
				tcs.TrySetException(e);
			}
			return await tcs.Task;
		}

		async void ReadNextResponse()
		{
			try
			{
				while (true)
				{
					var len = BitConverter.ToInt32(await _stream.ReadExactlyAsync(4).ConfigureAwait(false), 0);
					var packet = await _stream.ReadExactlyAsync(len);
					var rid = BitConverter.ToInt64(packet, 0);
					var data = new byte[len - 8];
					Array.Copy(packet, 8, data, 0, data.Length);
					lock (_syncRoot)
					{
						TaskCompletionSource<byte[]> tcs;
						if (!_pendingRequests.TryGetValue(rid, out tcs)) continue;
						_pendingRequests.Remove(rid);
						tcs.SetResult(data);
					}

				}
			}
			catch (Exception e)
			{
				lock (_syncRoot)
				{
					_ioException = e;
					foreach (var tcs in _pendingRequests.Values)
						tcs.TrySetException(e);
					_pendingRequests.Clear();
					Dispose();
				}
			}
		}


	}
}
