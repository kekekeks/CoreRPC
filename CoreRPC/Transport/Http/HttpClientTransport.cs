using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CoreRPC.Transport.Http
{
    public class HttpClientTransport
    {

        private readonly string _url;

        public HttpClientTransport( string url)
        {
            _url = url;
        }


        public Task<byte[]> SendMessageAsync(byte[] message)
        {
            var cl = new HttpClient();
            try
            {
                return ProcessResponseAsync(cl, cl.SendAsync(new HttpRequestMessage(HttpMethod.Post, this._url)
                {
                    Content = new ByteArrayContent(message)
                }));
            }
            catch
            {
                cl.Dispose();
                throw;
            }
        }

        async Task<byte[]> ProcessResponseAsync(HttpClient client, Task<HttpResponseMessage> task)
        {
            using (client)
            using (var res = await task)
            {
                if (!res.IsSuccessStatusCode)
                    throw new Exception("Server returned non-success status code: " + res.StatusCode);
                return await res.Content.ReadAsByteArrayAsync();
            }
        }
    }
}
