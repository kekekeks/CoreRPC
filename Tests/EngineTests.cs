using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreRPC;
using CoreRPC.Routing;
using CoreRPC.Transport;
using Xunit;

namespace Tests
{
	public class EngineTests
	{
		public interface ITarget
		{
			Task FooAsync();
			Task<string> BarAsync(string arg);
		}

		class Target : ITarget
		{
			public Task FooAsync()
			{
				return Task.FromResult(0);
			}

			public Task<string> BarAsync(string arg)
			{
				return Task.FromResult(arg + "1");
			}
		}

		private class Selector : ITargetSelector
		{
			public object GetTarget(string target)
			{
				return new Target();
			}
		}

		[Fact]
		public void TestCalls()
		{
			var task = TestCallAsync();
			task.Wait();
		}

		public async Task TestCallAsync ()
		{
			var engine = new Engine();
			var proxy = engine.CreateProxy<ITarget>(new InternalThreadPoolTransport(engine.CreateRequestHandler(new Selector())));

			await proxy.FooAsync();
			Assert.Equal("x1", await proxy.BarAsync("x"));
		}
	}
}
