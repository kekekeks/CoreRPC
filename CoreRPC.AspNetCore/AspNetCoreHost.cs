using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace CoreRPC.Transport.Http
{
    public static class CoreRPCAspNetCoreExtensions
    {
        class Request : IRequest
        {
            private readonly HttpContext _ctx;

            public Request(HttpContext ctx, byte[] data)
            {
                _ctx = ctx;
                Data = data;
            }
            public byte[] Data { get; }

            public Task RespondAsync(byte[] data) => _ctx.Response.Body.WriteAsync(data, 0, data.Length);
        }

        static async Task Handle(HttpContext context, IRequestHandler handler)
        {
            var ms = new MemoryStream();
            await context.Request.Body.CopyToAsync(ms);
            await handler.HandleRequest(new Request(context, ms.ToArray()));
        }

        public static IApplicationBuilder UseCoreRPC(this IApplicationBuilder builder, PathString path, IRequestHandler handler, Func<HttpContext, Func<Task>, Task> hook = null)
            => builder.Use((context, next) =>
            {
                if (context.Request.Path == path && context.Request.Method == "POST")
                {
                    if (hook == null)
                        return Handle(context, handler);
                    return hook.Invoke(context, () => Handle(context, handler));
                }
                return next();
            });
    }
}
