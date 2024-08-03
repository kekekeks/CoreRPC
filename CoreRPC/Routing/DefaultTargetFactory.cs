using System;
using System.Diagnostics.CodeAnalysis;

namespace CoreRPC.Routing
{
    public class DefaultTargetFactory : ITargetFactory
    {
        public object CreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type type, object callContext)
        {
            return Activator.CreateInstance(type);
        }
    }
}