using System;

namespace CoreRPC.Routing
{
    public interface ITargetFactory
    {
        object CreateInstance(Type type);
    }
}