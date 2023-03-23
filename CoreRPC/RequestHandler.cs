using System;
using System.IO;
using System.Threading.Tasks;
using CoreRPC.Binding;
using CoreRPC.Routing;
using CoreRPC.Serialization;
using CoreRPC.Transferable;
using CoreRPC.Transport;
using CoreRPC.Utility;
using Microsoft.IO;

namespace CoreRPC
{
    class RequestHandler : IRequestHandler
    {
        private readonly ITargetSelector _selector;
        private readonly IMethodBinder _binder;
        private readonly IMethodCallSerializer _serializer;
        private readonly IMethodCallInterceptor _interceptor;
        private readonly IRequestErrorHandler _errors;

        public RequestHandler(
            ITargetSelector selector,
            IMethodBinder binder,
            IMethodCallSerializer serializer,
            IMethodCallInterceptor interceptor,
            IRequestErrorHandler errors)
        {
            _selector = selector;
            _binder = binder;
            _serializer = serializer;
            _interceptor = interceptor;
            _errors = errors;
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
                if (req.Data is RecyclableMemoryStream || req.Data is MemoryStream)
                    call = _serializer.DeserializeCall(req.Data, _binder, _selector, req.Context);
                else
                {
                    using var copy = new RecyclableMemoryStream(StreamPool.Shared);
                    await req.Data.CopyToAsync(copy);
                    req.Data.Dispose();
                    copy.Position = 0;
                    call = _serializer.DeserializeCall(copy, _binder, _selector, req.Context);
                }
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
                    using (call)
                    {
                        if (_interceptor != null)
                        {
                            res = _interceptor.Intercept(call, req.Context,
                                () => ConvertToTask(call.Method.Invoke(call.Target, call.Arguments)));
                        }
                        else
                            res = call.Method.Invoke(call.Target, call.Arguments);
                    }
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
                        try
                        {
                            await task;
                            if (call.Method.ReturnType != typeof(Task))
                                result = task.GetType().GetProperty("Result")?.GetValue(task);
                        }
                        catch (Exception e)
                        {
                            ex = e;
                        }
                    }
                }
            }

            using var response = new RecyclableMemoryStream(StreamPool.Shared);
            if (ex == null)
            {
                try
                {
                    _serializer.SerializeResult(response, result);
                    response.Position = 0;
                }
                catch (Exception e)
                {
                    ex = e;
                }
            }

            if (ex != null)
            {
                if (_errors != null)
                {
                    try
                    {
                        var handled = _errors.HandleError(ex);
                        SerializeError(response, handled ?? "Internal Server Error");
                    }
                    catch
                    {
                        SerializeError(response, ex.ToString());
                    }
                }
                else
                {
                    SerializeError(response, ex.ToString());
                }
            }

            try
            {
                await req.RespondAsync(response);
            }
            catch (Exception fatal)
            {
                if (_errors != null)
                {
                    try
                    {
                        _errors.HandleError(fatal);
                    }
                    catch
                    {
                        // We've tried.
                    }
                }
            }
        }

        void SerializeError(RecyclableMemoryStream response, string error)
        {
            response.Position = 0;
            response.SetLength(0);
            _serializer.SerializeException(response, error);
            response.Position = 0;
        }
    }
}