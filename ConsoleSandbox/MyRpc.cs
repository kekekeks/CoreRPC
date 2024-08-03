using CoreRPC.Utility;
using System.Buffers;
using System.Runtime.InteropServices;

public class MyRpc : IMyRpc
{
    public async Task<BinaryResponse> Test(Stream foo, List<Stream> bar, List<Memory<byte>> memories,
        byte[] bytes)
    {
        var ms = new MemoryStream();
        foo.CopyTo(ms);
        foreach (var s in bar)
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
