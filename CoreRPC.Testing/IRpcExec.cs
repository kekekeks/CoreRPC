using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CoreRPC.Testing
{
    public interface IRpcExec<TRpc>
    {
        TResponse Call<TResponse>(Expression<Func<TRpc, TResponse>> expression);
        Task<TResponse> Call<TResponse>(Expression<Func<TRpc, Task<TResponse>>> expression);
    }

    public abstract class RpcListBase
    {
        private static readonly Lazy<HttpClient> SharedClient = new Lazy<HttpClient>(() => new HttpClient());
        private readonly Dictionary<string, string> _headers;
        private readonly JsonSerializerSettings _settings;
        private readonly HttpClient _http;
        private readonly string _baseUri;

        protected RpcListBase(
            string uri,
            Dictionary<string, string> headers,
            HttpClient http = null,
            JsonSerializerSettings settings = null)
        {
            _baseUri = uri;
            _headers = headers;
            _settings = settings;
            _http = http ?? SharedClient.Value;
        }

        protected IRpcExec<TRpc> Get<TRpc>() => new RpcExecHelper<TRpc>(_http, _baseUri, _headers, settings: _settings);
    }
}