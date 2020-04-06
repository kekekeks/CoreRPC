using System;
using System.Collections.Generic;
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

        public Task<byte[]> SendMessageAsync(byte[] message)
        {
            return ProcessResponseAsync(_client.SendAsync(new HttpRequestMessage(HttpMethod.Post, _url)
            {
                Content = new ByteArrayContent(message)
            }));
        }

        async Task<byte[]> ProcessResponseAsync(Task<HttpResponseMessage> task)
        {
            using (var res = await task)
            {
                if (!res.IsSuccessStatusCode)
                    throw new Exception("Server returned non-success status code: " + res.StatusCode);
                return await res.Content.ReadAsByteArrayAsync();
            }
        }
    }
}
