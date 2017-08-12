using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncRpc.Transport
{
	public interface IRequest
	{
		byte[] Data { get; }
		Task RespondAsync(byte[] data);
	}
}
