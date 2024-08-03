using System;
using System.Diagnostics.CodeAnalysis;

namespace CoreRPC.Routing
{
    public interface ITargetFactory
    {
        object CreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type type, object callContext);
    }
}