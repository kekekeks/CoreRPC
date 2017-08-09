using System;
using System.IO;
using System.Threading.Tasks;
using AsyncRpc.Binding;
using AsyncRpc.Routing;
using AsyncRpc.Serialization;
using AsyncRpc.Transferable;
using AsyncRpc.Transport;
using System.Reflection;

namespace AsyncRpc
{
	class RequestHandler : IRequestHandler
	{
		private readonly ITargetSelector _selector;
		private readonly IMethodBinder _binder;
		private readonly IMethodCallSerializer _serializer;

		public RequestHandler(ITargetSelector selector, IMethodBinder binder, IMethodCallSerializer serializer)
		{
			_selector = selector;
			_binder = binder;
			_serializer = serializer;
		}

		async Task IRequestHandler.HandleRequest (IRequest req)
		{
			Exception ex = null;
			object result = null;
			MethodCall call = null;
			try
			{
				call = _serializer.DeserializeCall(new MemoryStream(req.Data), _binder, _selector);
			}
			catch (Exception e)
			{
				ex = e;
			}
			if (call != null)
			{
				object res = null;
				try
				{
					res = call.Method.Invoke(call.Target, call.Arguments);
				}
				catch (Exception e)
				{
					ex = e;
				}
				if (res != null)
				{
					var task = res as Task;
					if (task == null)
						result = res;
					else
					{
						await task;
						if (call.Method.ReturnType != typeof (Task))
							result = task.GetType().GetTypeInfo().GetDeclaredProperty("Result").GetValue(task);
					}
				}
			}

			byte[] response = null;
			if (ex == null)
			{
				try
				{
					response = Serialize(s => _serializer.SerializeResult(s, result));
				}
				catch (Exception e)
				{
					ex = e;
				}
			}
			if (ex != null)
				response = Serialize(s => _serializer.SerializeException(s, ex.ToString()));
			try
			{
				await req.RespondAsync(response);
			}
			catch
			{
				//TODO: redirect it somewhere?
			}
		}

		private static byte[] Serialize(Action<MemoryStream> cb)
		{
			var ms = new MemoryStream();
			cb(ms);
			return ms.ToArray();
		}

	}
}