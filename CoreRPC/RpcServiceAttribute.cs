using System;
using System.Diagnostics.CodeAnalysis;
using CoreRPC.CodeGen;

namespace CoreRPC;

[AttributeUsage(AttributeTargets.Interface)]
public sealed class RpcServiceAttribute : Attribute
{

}

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class RpcServiceProxyAttribute<TInterface>(
#if NET6_0_OR_GREATER
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
    Type proxyType) : Attribute
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    public Type ProxyProxyType { get; } = proxyType;

    public TInterface CreateProxy(IRealProxy realProxy)
    {
        return (TInterface)ProxyProxyType.GetConstructor([typeof(IRealProxy)])!.Invoke([realProxy]);
    }
}
