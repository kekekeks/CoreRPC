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
using Microsoft.IO;

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

        public async Task<T> Invoke<T>(MethodInfo method, IEnumerable args)
        {
            using var ms = new RecyclableMemoryStream(StreamPool.Shared);
            _serializer.SerializeCall(ms, _binder, _targetName, new MethodCall
                {
                    Method = method,
                    Arguments = args.Cast<object>().ToArray()
                });
            ms.Position = 0;
            var res = await SendAndParseResponse(ms, typeof(T));
            if(res.Exception != null)
                throw new Exception(res.Exception);
            return (T) res.Result;
        }

        async Task<MethodCallResult> SendAndParseResponse(Stream data, Type expectedType)
        {
            using (var resp = await _transport.SendMessageAsync(data))
                return _serializer.DeserializeResult(resp, expectedType);
        }
    }
}
