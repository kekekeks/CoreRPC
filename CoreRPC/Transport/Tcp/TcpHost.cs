using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace CoreRPC.Transport.Tcp
{
	public class TcpHost : IDisposable
	{
		private TcpListener _listener;
		private readonly IRequestHandler _engine;
		private readonly object _syncRoot = new object();

		public delegate void NetworkErrorHandler(TcpHost host, Exception e);

		public event NetworkErrorHandler NetworkError;

		public TcpHost(IRequestHandler engine)
		{
			if (engine == null) throw new ArgumentNullException("engine");
			_engine = engine;
		}

		public void StartListening(IPEndPoint endPoint)
		{
			lock (_syncRoot)
			{
				if (_listener != null)
					throw new InvalidOperationException();
				_listener = new TcpListener(endPoint);
				_listener.Start();
				HandleNextConnection ();
			}
		}

		async void HandleNextConnection()
		{
			Task<TcpClient> task;
			lock (_syncRoot)
			{
				if (_listener == null)
					return;
				task = _listener.AcceptTcpClientAsync ();
			}
			TcpClient cl;
			try
			{
				cl = await task;
			}
			catch
			{
				//
				return;
			}
			HandleNextConnection();
			TcpHostConnection.HandleNew(this, cl, _engine);
		}

		public void StopListening()
		{
			lock (_syncRoot)
			{
				if (_listener == null)
					throw new InvalidOperationException();
				Dispose();
			}
		}

		internal void FireNetworkError(Exception e)
		{
			NetworkError(this, e);
		}

		public void Dispose()
		{
			lock (_syncRoot)
			{
				if (_listener == null) return;
				_listener.Stop();
				_listener = null;
			}
		}
	}
}
