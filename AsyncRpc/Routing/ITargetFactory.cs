using System;

namespace AsyncRpc.Routing
{
	public interface ITargetFactory
	{
		object CreateInstance(Type type);
	}
}