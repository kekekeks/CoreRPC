using System.Threading.Tasks;

namespace CoreRPC.Transport
{
	public interface IRequestHandler
	{
		Task HandleRequest(IRequest req);
	}
}
