using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using CoreRPC;
using CoreRPC.Binding.Default;
using CoreRPC.JsonLikeBinarySerializer;
using CoreRPC.Routing;
using CoreRPC.Transport;
using CoreRPC.Utility;
using Microsoft.IO;
using Xunit;

namespace Tests
{
    public class BinarySerializedEnd2EndTests
    {
        public class BinaryResponse
        {
            public IMemoryOwner<byte> Memory { get; set; }
            public Stream Stream { get; set; }
            public byte[] Bytes { get; set; }
        }
        
        public interface IMyRpc
        {
            Task<BinaryResponse> Test(Stream foo, List<Stream> bar, List<Memory<byte>> memories, byte[] bytes);
        }
        
        public class MyRpc : IMyRpc
        {
            public async Task<BinaryResponse> Test(Stream foo, List<Stream> bar, List<Memory<byte>> memories,
                byte[] bytes)
            {
                var ms = new MemoryStream();
                foo.CopyTo(ms);
                foreach(var s in bar)
                    s.CopyTo(ms);
                foreach (var mem in memories)
                {
                    MemoryMarshal.TryGetArray(mem, out ArraySegment<byte> seg);
                    ms.Write(seg.Array, seg.Offset, seg.Count);
                }

                ms.Write(bytes, 0, bytes.Length);

                ms.Position = 0;
                var rev = ms.ToArray().Reverse().ToArray();
                return new BinaryResponse
                {
                    Stream = ms,
                    Memory = new ArrayMemoryOwner(rev),
                    Bytes = rev
                };
            }
        }
        

        [Fact]
        public void ModernMemoryObjectsCanBePassedViaRpc()
        {
            var engine = new Engine(new BinaryJsonLikeMethodCallSerializer(), new DefaultMethodBinder());
            var handler = engine.CreateProxy<IMyRpc>(
                    new InternalThreadPoolTransport(engine.CreateRequestHandler(new Selector())));

            var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes("Stream1"));
            var memoryStreams = new List<Stream>
            {
                new MemoryStream(Encoding.UTF8.GetBytes("Stream2"))
            };
            var result = handler.Test(memoryStream, memoryStreams, new List<Memory<byte>>
            {
                new Memory<byte>(Encoding.UTF8.GetBytes("Memory1")),
                new Memory<byte>(Encoding.UTF8.GetBytes("Memory2"))
            }, Encoding.UTF8.GetBytes("Bytes")).Result;
            Assert.Equal("Stream1Stream2Memory1Memory2Bytes", Encoding.UTF8.GetString(
                ((RecyclableMemoryStream)result.Stream).ReadAsBytes()));
            Assert.Equal("Stream1Stream2Memory1Memory2Bytes".Reverse(), Encoding.UTF8.GetString(
                result.Memory.Memory.ToArray()));
            Assert.Equal("Stream1Stream2Memory1Memory2Bytes".Reverse(), Encoding.UTF8.GetString(
                result.Bytes));
        }
        
        
        class Selector : ITargetSelector
        {
            public object GetTarget(string target, object callContext)
            {
                return new MyRpc();
            }
        }
    }
}