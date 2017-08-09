using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AsyncRpc.Binding;
using AsyncRpc.Binding.Default;
using Xunit;

namespace Tests
{
	public class DefaultBinderTests
	{
		public class Target
		{
			public int Foo(int x)
			{
				return x;
			}

			public int Foo(int x, string y)
			{
				return x + int.Parse(y);
			}

			public void Bar(int z)
			{
				if (z != 5)
					throw new InvalidOperationException();
			}

		}

		[Fact]
		public void TestSaveAndBind()
		{
			var target = new Target();
			var binder = (IMethodBinder) new DefaultMethodBinder();

			var fooX = binder.GetMethodSignature(typeof (Target).GetMethod("Foo", new[] {typeof (int)}));
			var fooXy = binder.GetMethodSignature(typeof (Target).GetMethod("Foo", new[] {typeof (int), typeof (string)}));
			var bar = binder.GetMethodSignature(typeof (Target).GetMethod("Bar"));

			var info = binder.GetInfoProviderFor(target);
			Assert.Equal(1, info.GetMethod(fooX).Invoke(target, new object[] {1}));
			Assert.Equal (3, info.GetMethod (fooXy).Invoke (target, new object[] { 1, "2" }));
			info.GetMethod (bar).Invoke (target, new object[] { 5 });
		}


	}
}