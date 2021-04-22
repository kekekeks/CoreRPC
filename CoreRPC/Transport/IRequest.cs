using System.IO;
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
