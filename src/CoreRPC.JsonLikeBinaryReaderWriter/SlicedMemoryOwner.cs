using System.Buffers;

namespace CoreRPC.JsonLikeBinaryReaderWriter;

internal class SlicedMemoryOwner : IMemoryOwner<byte>
{
    private readonly IMemoryOwner<byte> _inner;
    private readonly int _size;

    public SlicedMemoryOwner(IMemoryOwner<byte> inner, int size)
    {
        _inner = inner;
        _size = size;
    }
    public void Dispose() => _inner.Dispose();

    public Memory<byte> Memory => _inner.Memory.Slice(0, _size);
}