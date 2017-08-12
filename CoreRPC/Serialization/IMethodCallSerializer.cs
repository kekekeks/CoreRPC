using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CoreRPC.Binding;
using CoreRPC.Routing;
using CoreRPC.Transferable;

namespace CoreRPC.Serialization
{
	public interface IMethodCallSerializer
	{
		void SerializeCall(Stream stream, IMethodBinder binder, string target, MethodCall call);
		MethodCall DeserializeCall(Stream stream, IMethodBinder binder, ITargetSelector selector);
		void SerializeResult(Stream stream, object result);
		void SerializeException(Stream stream, string exception);
		
		MethodCallResult DeserializeResult(Stream stream, Type expectedType);
	}
}
