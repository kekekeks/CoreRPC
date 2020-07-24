using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CoreRPC.Utility;
using Microsoft.IO;

namespace CoreRPC.Transport
{
    public class InternalThreadPoolTransport : IClientTransport
    {
        private readonly IRequestHandler _engine;

        public InternalThreadPoolTransport(IRequestHandler engine)
        {
            _engine = engine;
        }

        class Request : IRequest
        {
            private readonly TaskCompletionSource<Stream> _tcs;

            public Request(TaskCompletionSource<Stream> tcs, Stream data)
            {
                _tcs = tcs;
                Data = data;
            }

            public Stream Data { get; private set; }
            public object Context { get; } = null;
            public async Task RespondAsync(Stream data)
            {
                var ms = new RecyclableMemoryStream(StreamPool.Shared);
                await data.CopyToAsync(ms);
                ms.Position = 0;
                _tcs.SetResult(ms);
            }
        }

        public Task<Stream> SendMessageAsync(Stream message)
        {
            var tcs = new TaskCompletionSource<Stream>();
            Task.Run(() => _engine.HandleRequest(new Request(tcs, message)));
            return tcs.Task;
        }
    }
}
