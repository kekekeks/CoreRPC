using System;
using System.IO;
using System.Threading.Tasks;
using CoreRPC.Binding;
using CoreRPC.Routing;
using CoreRPC.Serialization;
using CoreRPC.Transferable;
using CoreRPC.Transport;
using System.Reflection;

namespace CoreRPC
{
    class RequestHandler : IRequestHandler
    {
        private readonly ITargetSelector _selector;
        private readonly IMethodBinder _binder;
        private readonly IMethodCallSerializer _serializer;
        private readonly IMethodCallInterceptor _interceptor;

        public RequestHandler(ITargetSelector selector, IMethodBinder binder,
            IMethodCallSerializer serializer, IMethodCallInterceptor interceptor)
        {
            _selector = selector;
            _binder = binder;
            _serializer = serializer;
            _interceptor = interceptor;
        }

        static async Task<object> ConvertToTask(object ires)
        {
            if (ires is Task task)
            {
                await task;
                var prop = task.GetType().GetProperty("Result");
                return prop?.GetValue(task);
            }
            return ires;
        }

        async Task IRequestHandler.HandleRequest (IRequest req)
        {
            Exception ex = null;
            object result = null;
            MethodCall call = null;
            try
            {
                call = _serializer.DeserializeCall(new MemoryStream(req.Data), _binder, _selector, req.Context);
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
                    if (_interceptor != null)
                    {
                        res = _interceptor.Intercept(call, req.Context,
                            () => ConvertToTask(call.Method.Invoke(call.Target, call.Arguments)));
                    }
                    else
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
                            result = task.GetType().GetProperty("Result")?.GetValue(task);
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