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
            return _client.SendAsync(new HttpRequestMessage(HttpMethod.Post, _url)
            {
                Content = new ByteArrayContent(message)
            }).ContinueWith(ReadByteResponse).Unwrap();
        }

        // This oddity is needed to avoid catching `byte[] message` in the async state machine
        // That was causing 6GB resource leak in one of our applications
        private async Task<byte[]> ReadByteResponse(Task<HttpResponseMessage> t)
        {
            using (t.Result)
            {
                if (t.Result.IsSuccessStatusCode)
                    throw new Exception("Server returned non-success status code: " + t.Result.StatusCode);
                return await t.Result.Content.ReadAsByteArrayAsync();
            }
        }
    }
}
