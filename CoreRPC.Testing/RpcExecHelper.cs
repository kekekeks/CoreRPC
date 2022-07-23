using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Threading.Tasks;
using CoreRPC.AspNetCore;
using CoreRPC.Binding;
using CoreRPC.Binding.Default;
using CoreRPC.Routing;
using Newtonsoft.Json;

namespace CoreRPC.Testing
{
    public class RpcExecHelper<TRpc> : IRpcExec<TRpc>, IDisposable
    {
        private readonly Dictionary<string, string> _headers;
        private readonly JsonSerializerSettings _settings;
        private readonly ITargetNameExtractor _extractor;
        private readonly IMethodBinder _binder;
        private readonly HttpClient _http;
        private readonly string _uri;

        public RpcExecHelper(
            HttpClient http,
            string uri,
            Dictionary<string, string> headers,
            ITargetNameExtractor extractor = null,
            IMethodBinder binder = null,
            JsonSerializerSettings settings = null)
        {
            _http = http;
            _uri = uri;
            _headers = headers;
            _settings = settings;
            _extractor = extractor ?? new AspNetCoreTargetNameExtractor();
            _binder = binder ?? new DefaultMethodBinder();
        }

        public TRes Call<TRes>(Expression<Func<TRpc, TRes>> cb)
        {
            var response = CallImpl(cb);
            var deserialized = JsonConvert.DeserializeObject<ResultResponse<TRes>>(response, _settings);
            if (deserialized is null)
                throw new InvalidOperationException($"Received null value when trying to deserialize {response}");
            var instance = deserialized.Result;
            return instance;
        }

        public Task<TRes> Call<TRes>(Expression<Func<TRpc, Task<TRes>>> cb)
        {
            var response = CallImpl(cb);
            var deserialized = JsonConvert.DeserializeObject<ResultResponse<TRes>>(response, _settings);
            if (deserialized is null)
                throw new InvalidOperationException($"Received null value when trying to deserialize {response}");
            var instance = deserialized.Result;
            return Task.FromResult(instance);
        }

        private string CallImpl<TRes>(Expression<Func<TRpc, TRes>> cb)
        {
            if (cb.Parameters.Count != 1)
                throw new InvalidOperationException();
            var mcall = (MethodCallExpression) cb.Body;
            var sig = _binder.GetMethodSignature(mcall.Method);
            var args = mcall.Arguments
                .Select(argument =>
                    Expression.Lambda<Func<object>>(
                            Expression.Convert(argument, typeof(object)))
                        .Compile()
                        .Invoke())
                .ToList();

            var name = _extractor.GetTargetName(cb.Parameters[0].Type);
            var content = new StringContent(JsonConvert.SerializeObject(new
            {
                Target = name,
                MethodSignature = sig,
                Arguments = args
            }));

            if (_headers != null)
                foreach (var hdr in _headers)
                    content.Headers.Add(hdr.Key, hdr.Value);

            using (var response = _http.PostAsync(_uri, content).Result)
            {
                var json = response.Content.ReadAsStringAsync().Result;
                if (!response.IsSuccessStatusCode)
                    throw new Exception("Non-zero status code " + response.StatusCode + ":\n\n" + json);
                var error = JsonConvert.DeserializeObject<ExceptionResponse>(json);
                if (error?.Exception != null)
                    throw new Exception(error.Exception);
                return json;
            }
        }

        public void Dispose() => _http.Dispose();

        private class ResultResponse<T>
        {
            public T Result { get; set; }
        }

        private class ExceptionResponse
        {
            public string Exception { get; set; }
        }
    }
} 
