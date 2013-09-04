using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncRpc.Transport
{
	public interface IClientTransport
	{
		Task<byte[]> SendMessageAsync(byte[] message);
	}
}
