using System.Threading.Tasks;

namespace AsyncRpc.Transport
{
	public interface IRequestHandler
	{
		Task HandleRequest(IRequest req);
	}
}
