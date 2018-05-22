using System.Threading;
using System.Threading.Tasks;

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
            private readonly TaskCompletionSource<byte[]> _tcs;

            public Request(TaskCompletionSource<byte[]> tcs, byte[] data)
            {
                _tcs = tcs;
                Data = data;
            }

            public byte[] Data { get; private set; }
            public object Context { get; } = null;
            public Task RespondAsync(byte[] data)
            {
                _tcs.SetResult(data);
                return Task.Run(() => { });
            }
        }

        public Task<byte[]> SendMessageAsync(byte[] message)
        {
            var tcs = new TaskCompletionSource<byte[]>();
            Task.Run(() => _engine.HandleRequest(new Request(tcs, message)));
            return tcs.Task;
        }
    }
}
