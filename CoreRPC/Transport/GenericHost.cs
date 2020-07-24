using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRPC.Transport
{
    public class GenericHost
    {
        private readonly IRequestHandler _handler;

        private class Request : IRequest
        {
            private readonly TaskCompletionSource<Stream> _tcs;

            public Request(Stream data, TaskCompletionSource<Stream> tcs, object context = null)
            {
                Data = data;
                Context = context;
                _tcs = tcs;
            }

            public object Context { get; }

            public Task RespondAsync(Stream data)
            {
                _tcs.SetResult(data);
                return Task.FromResult(0);
            }

            public Stream Data { get; private set; }
        }

        public GenericHost(IRequestHandler handler)
        {
            _handler = handler;
        }

        public Task<Stream> HandleRequest(Stream data)
        {
            var tcs = new TaskCompletionSource<Stream>();
            _handler.HandleRequest(new Request(data, tcs));
            return tcs.Task;
        }
    }
}
