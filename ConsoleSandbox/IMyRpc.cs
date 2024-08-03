using CoreRPC;
using System.Buffers;

[RpcService]
public interface IMyRpc
{
    Task<BinaryResponse> Test(Stream foo, List<Stream> bar, List<Memory<byte>> memories, byte[] bytes);
}

public class BinaryResponse
{
    public IMemoryOwner<byte> Memory { get; set; }
    public Stream Stream { get; set; }
    public byte[] Bytes { get; set; }
}