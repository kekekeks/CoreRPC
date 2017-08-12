using System.Reflection;

namespace CoreRPC.Binding
{
	public interface IMethodInfoProvider
	{
		MethodInfo GetMethod(byte[] signature);
	}
}
