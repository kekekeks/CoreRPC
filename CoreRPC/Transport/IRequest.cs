using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRPC.Transport
{
    public interface IRequest
    {
        Stream Data { get; }
        object Context { get; }
        Task RespondAsync(Stream data);
    }
}
