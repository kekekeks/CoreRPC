using System;

namespace CoreRPC.Routing
{
	public class DefaultTargetFactory : ITargetFactory
	{
		public object CreateInstance(Type type)
		{
			return Activator.CreateInstance(type);
		}
	}
}