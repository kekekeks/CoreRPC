using System.Reflection;

namespace AsyncRpc.Binding
{
	public interface IMethodInfoProvider
	{
		MethodInfo GetMethod(byte[] signature);
	}
}
