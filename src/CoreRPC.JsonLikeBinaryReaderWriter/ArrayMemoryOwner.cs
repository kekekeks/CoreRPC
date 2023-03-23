using System.Buffers;

namespace CoreRPC.Utility;

public class ArrayMemoryOwner : IMemoryOwner<byte>
{
    public ArrayMemoryOwner(byte[] data)
    {
        Memory = data;
    }

    public ArrayMemoryOwner(ArraySegment<byte> segment)
    {
        Memory = segment;
    }
    
    public void Dispose()
    {
        
    }

    public Memory<byte> Memory { get; }
}