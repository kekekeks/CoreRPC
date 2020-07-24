using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRPC.Transport
{
    public interface IClientTransport
    {
        Task<Stream> SendMessageAsync(Stream message);
    }
}
