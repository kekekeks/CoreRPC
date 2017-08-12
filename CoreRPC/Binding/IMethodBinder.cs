using System.Reflection;

namespace CoreRPC.Binding
{
    public interface IMethodBinder
    {
        IMethodInfoProvider GetInfoProviderFor(object obj);
        byte[] GetMethodSignature (MethodInfo nfo);
    }
}
