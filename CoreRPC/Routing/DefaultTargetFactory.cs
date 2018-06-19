using System;

namespace CoreRPC.Routing
{
    public class DefaultTargetFactory : ITargetFactory
    {
        public object CreateInstance(Type type, object callContext)
        {
            return Activator.CreateInstance(type);
        }
    }
}