using System.Reflection;

namespace AsyncRpc.Binding
{
	public interface IMethodBinder
	{
		IMethodInfoProvider GetInfoProviderFor(object obj);
		byte[] GetMethodSignature (MethodInfo nfo);
	}
}
