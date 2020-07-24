using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CoreRPC.Transport.Http
{
    public class HttpClientTransport : IClientTransport
    {
        private readonly HttpClient _client;
        private readonly string _url;

        public HttpClientTransport(HttpClient client, string url)
        {
            _client = client;
            _url = url;
        }

        public HttpClientTransport(string url) : this(new HttpClient(), url)
        {
            
        }

        public Task<Stream> SendMessageAsync(Stream message)
        {
            return ProcessResponseAsync(_client.SendAsync(new HttpRequestMessage(HttpMethod.Post, _url)
            {
                Content = new StreamContent(message)
            }));
        }

        async Task<Stream> ProcessResponseAsync(Task<HttpResponseMessage> task)
        {
            var res = await task;
            var success = false;
            try
            {
                if (!res.IsSuccessStatusCode)
                    throw new Exception("Server returned non-success status code: " + res.StatusCode);
                var rv = await res.Content.ReadAsStreamAsync();
                success = true;
                return rv;
            }
            finally
            {
                if(!success)
                    res.Dispose();
            }
        }
    }
}
