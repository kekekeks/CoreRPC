using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CoreRPC.Binding;
using CoreRPC.CodeGen;
using CoreRPC.Routing;
using CoreRPC.Serialization;
using CoreRPC.Transferable;
using CoreRPC.Transport;
using CoreRPC.Utility;

namespace CoreRPC
{
    class CallProxy : IRealProxy
    {
        private readonly IClientTransport _transport;
        private readonly IMethodCallSerializer _serializer;
        private readonly IMethodBinder _binder;
        private readonly string _targetName;

        public CallProxy(IClientTransport transport, IMethodCallSerializer serializer, IMethodBinder binder, string targetName)
        {
            _transport = transport;
            _serializer = serializer;
            _binder = binder;
            _targetName = targetName;
        }

        public object Invoke(MethodInfo method, IEnumerable args)
        {
            var ms = new MemoryStream();
            Type expectedType = null;
            _serializer.SerializeCall(ms, _binder, _targetName, new MethodCall
                {
                    Method = method,
                    Arguments = args.Cast<object>().ToArray()
                });

            ITaskCompletionSource tcs;
            if (method.ReturnType == typeof (Task))
                tcs = new TcsWrapper<object> ();
            else if (method.ReturnType.GetTypeInfo().IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof (Task<>))
            {
                expectedType = method.ReturnType.GetGenericArguments()[0];
                tcs = (ITaskCompletionSource)
                      Activator.CreateInstance(
                          typeof (TcsWrapper<>).MakeGenericType(expectedType));
            }
            else
                throw new InvalidOperationException("Non Task/Task<T> return values aren't supported");

            SendAndParseResponse(ms.ToArray(), expectedType).ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        tcs.SetException(t.Exception);
                        return;
                    }
                    var rslt = t.Result;
                    if (rslt.Exception != null)
                        tcs.SetException(new Exception(rslt.Exception));
                    else
                        tcs.SetResultOrCastException(rslt.Result);
                });
            return tcs.Task;
        }

        async Task<MethodCallResult> SendAndParseResponse(byte[] data, Type expectedType)
        {
            var resp = new MemoryStream(await _transport.SendMessageAsync(data));
            return _serializer.DeserializeResult(resp, expectedType);
        }
    }
}
