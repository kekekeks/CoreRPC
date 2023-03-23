using System.Buffers;
using Microsoft.IO;

namespace CoreRPC.JsonLikeBinaryReaderWriter;


public interface IBinaryJsonLikeMemoryPool
{
    IMemoryOwner<byte> RentMemory(int len);
    RecyclableMemoryStream RentStream(int len);
    void ReturnAndDispose(IDisposable rented);
}

public class ArenaBinaryJsonLikeMemoryPool : IBinaryJsonLikeMemoryPool, IDisposable
{
    private readonly MemoryPool<byte> _memoryPool;
    private static RecyclableMemoryStreamManager SharedStreamManager = new();
    private readonly ArrayPool<byte> _arrayPool;
    private readonly RecyclableMemoryStreamManager _streamPool;
    private object _lock = new();
    private HashSet<IDisposable> _rented = new();
    
    public ArenaBinaryJsonLikeMemoryPool(MemoryPool<byte>? memoryPool, ArrayPool<byte>? arrayPool, RecyclableMemoryStreamManager? streamPool)
    {
        _memoryPool = memoryPool ?? MemoryPool<byte>.Shared;
        _arrayPool = arrayPool ?? ArrayPool<byte>.Shared;
        _streamPool = streamPool ?? SharedStreamManager;
    }   
    
    public void Dispose()
    {
        lock (_lock)
        {
            foreach (var v in _rented) 
                v.Dispose();
            _rented.Clear();
        }
    }

    public IMemoryOwner<byte> RentMemory(int len)
    {
        var mem = _memoryPool.Rent(len);
        lock (_lock)
            _rented.Add(mem);
        return mem;
    }

    public RecyclableMemoryStream RentStream(int len)
    {
        var ms = new RecyclableMemoryStream(_streamPool, null, len);
        lock (_lock)
            _rented.Add(ms);
        return ms;
    }

    public void ReturnAndDispose(IDisposable rented)
    {
        lock (_lock)
            _rented.Remove(rented);
        rented.Dispose();
    }
}