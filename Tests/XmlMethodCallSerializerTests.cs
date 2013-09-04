using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AsyncRpc;
using AsyncRpc.Binding;
using AsyncRpc.Binding.Default;
using AsyncRpc.CodeGen;
using AsyncRpc.Routing;
using AsyncRpc.Serialization;
using AsyncRpc.Transferable;
using Xunit;

namespace Tests
{
	public class XmlMethodCallSerializerTests
	{
		public interface IFoo
		{
			void Test(int x, List<string> y);
		}

		private class Target : IFoo
		{
			public void Test(int x, List<string> y)
			{
				Assert.Equal(1, x);
				Assert.Equal("2", y[0]);
				Assert.Equal ("3", y[1]);
			}
		}

		private class TargetSelector : ITargetSelector
		{
			private readonly Target _target;

			public TargetSelector(Target target)
			{
				_target = target;
			}

			public object GetTarget(string target)
			{
				Assert.Equal("Target", target);
				return _target;
			}
		}

		private class Proxy : IRealProxy
		{
			public MethodCall LastCall;
			public object Invoke(MethodInfo method, IEnumerable args)
			{
				LastCall = new MethodCall {Method = method, Arguments = args.Cast<object>().ToArray()};
				return null;
			}
		}


		[Fact]
		public void TestSerializeAndDeserialize()
		{
			var proxy = new Proxy();
			var tproxy = ProxyGen.CreateInstance<IFoo>(proxy);
			tproxy.Test(1, new List<string> {"2", "3"});
			var ser = new XmlMethodCallSerializer();
			var binder = new DefaultMethodBinder();
			var ms = new MemoryStream();

			ser.SerializeCall(ms, binder, "Target", proxy.LastCall);
			ms.Seek(0, SeekOrigin.Begin);
			
			var call = ser.DeserializeCall(ms, binder, new TargetSelector(new Target()));
			call.Method.Invoke(call.Target, call.Arguments);
		}

		MethodCallResult Reserialize (object obj, Type expectedType)
		{
			var ser = new XmlMethodCallSerializer ();
			var ms = new MemoryStream();
			ser.SerializeResult(ms, obj);
			ms.Seek(0, SeekOrigin.Begin);
			return ser.DeserializeResult(ms, expectedType);
		}

		MethodCallResult ReserializeException (string ex)
		{
			var ser = new XmlMethodCallSerializer ();
			var ms = new MemoryStream ();
			ser.SerializeException(ms, ex);
			ms.Seek (0, SeekOrigin.Begin);
			return ser.DeserializeResult(ms, typeof (string));
		}

		[Fact]
		public void TestResultSerialization()
		{
			var emptyResult = Reserialize(null, null);
			Assert.Null (emptyResult.Exception);
			Assert.Null (emptyResult.Result);

			var exceptionResult = ReserializeException("fail");
			Assert.Null(exceptionResult.Result);
			Assert.Equal("fail", exceptionResult.Exception);

			var valueResult = Reserialize(new List<string> {"success"}, typeof (List<string>));
			Assert.Null(valueResult.Exception);
			Assert.Equal("success", ((List<string>) valueResult.Result)[0]);






		}
		



	}
}
