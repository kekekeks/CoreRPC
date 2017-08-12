using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRPC.Transport
{
	public interface IClientTransport
	{
		Task<byte[]> SendMessageAsync(byte[] message);
	}
}
