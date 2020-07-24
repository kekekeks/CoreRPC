using System.IO;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Hosting;

namespace Tests
{
    public static class Utils
    {
        public static MemoryStream ReadAsMemoryStream(this Stream s)
        {
            var ms = new MemoryStream();
            s.CopyTo(ms);
            return ms;
        }

        public static byte[] ReadAsBytes(this Stream s)
        {
            return ReadAsMemoryStream(s).ToArray();
        }
        
        public static int GetFreePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint) listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        public static IWebHostBuilder UseFreePort(this IWebHostBuilder builder)
            => builder.UseUrls("http://127.0.0.1:" + GetFreePort() + "/");

        public static MemoryStream AsMemoryStream(this byte[] bytes) => new MemoryStream(bytes);
    }
}