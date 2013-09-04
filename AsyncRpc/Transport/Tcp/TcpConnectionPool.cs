using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AsyncRpc.Transport.Tcp
{
	internal class TcpConnectionPool
	{
		private readonly List<TcpRemote> _remotes;
		private readonly object _syncRoot = new object();
		private readonly List<TcpClientConnection> _pool = new List<TcpClientConnection>();

		public TcpConnectionPool(IEnumerable<TcpRemote> remotes)
		{
			_remotes = remotes.ToList();
		}

		public int Count
		{
			get { return _pool.Count; }
		}

		public TcpClientConnection GetConnection(bool remove)
		{
			lock (_pool)
			{
				if (_pool.Count == 0)
					throw new InvalidOperationException("Pool is empty");
				var connection = _pool[0];
				_pool.RemoveAt(0);
				if (!remove)
					_pool.Add(connection);
				return connection;
			}
		}

		public async Task<TcpClientConnection> GetNewConnection(bool addToPool)
		{
			var exceptions = new List<Exception>();
			foreach (var tcpRemote in _remotes)
			{
				try
				{
					var conn = await TcpClientConnection.ConnectAsync(tcpRemote.Host, tcpRemote.Port);
					if (addToPool)
						_pool.Add(conn);
				}
				catch (Exception e)
				{
					exceptions.Add(e);
				}
			}
			throw new AggregateException("Unable to connect to any of the specified hosts", exceptions);
		}

		public void RemoveConnection(TcpClientConnection conn)
		{
			lock (_syncRoot)
			{
				_pool.Remove(conn);
			}
		}

		public void AddConnection(TcpClientConnection conn)
		{
			lock (_syncRoot)
			{
				_pool.Add(conn);
			}
		}
	}
}
