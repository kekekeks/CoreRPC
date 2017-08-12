using System.Reflection;

namespace CoreRPC.Transferable
{
	public class MethodCall
	{
		public object Target { get; set; }
		public MethodInfo Method { get; set; }
		public object[] Arguments { get; set; }
	}
}
