using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AsyncRpc.Transport.Tcp
{
	class TcpConnection
	{
		private readonly object _syncRoot = new object ();
		private volatile bool _nowWriting;
		private readonly Queue<QueuedData> _sendQueue = new Queue<QueuedData> ();
		private readonly NetworkStream _stream;

		internal TcpConnection(NetworkStream stream)
		{
			_stream = stream;
		}

		private async void SendNextBuffer ()
		{
			QueuedData buffer;
			lock (_syncRoot)
			{
				if (_nowWriting)
					return;
				if (_sendQueue.Count == 0)
					return;
				buffer = _sendQueue.Dequeue ();
				_nowWriting = true;
			}

			try
			{
				await new MemoryStream (buffer.Buffer).CopyToAsync (_stream);
			}
			catch (Exception e)
			{
				buffer.Tcs.SetException (e);
			}
			buffer.Tcs.SetResult (0);

			lock (_syncRoot)
				_nowWriting = false;
			SendNextBuffer ();
		}

		protected Task SendBytesAsync (byte[] bytes)
		{
			var data = new QueuedData (bytes);
			lock (_syncRoot)
			{
				_sendQueue.Enqueue (data);
				SendNextBuffer ();
			}
			return data.Tcs.Task;
		}

		private class QueuedData
		{
			public readonly TaskCompletionSource<int> Tcs = new TaskCompletionSource<int> ();
			public readonly byte[] Buffer;

			public QueuedData (byte[] data)
			{
				Buffer = data;
			}
		}

	}
}
