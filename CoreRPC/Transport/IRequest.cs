using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRPC.Transport
{
    public interface IRequest
    {
        byte[] Data { get; }
        object Context { get; }
        Task RespondAsync(byte[] data);
    }
}
