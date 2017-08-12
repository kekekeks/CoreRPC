namespace AsyncRpc.Routing
{
	public interface ITargetSelector
	{
		object GetTarget(string target);
	}
}
