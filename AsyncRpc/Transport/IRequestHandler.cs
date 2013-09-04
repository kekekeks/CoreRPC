namespace AsyncRpc.Transport
{
	public interface IRequestHandler
	{
		void HandleRequest(IRequest req);
	}
}
