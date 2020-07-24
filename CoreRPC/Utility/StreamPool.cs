using Microsoft.IO;

namespace CoreRPC.Utility
{
    static class StreamPool
    {
        public static RecyclableMemoryStreamManager Shared { get; } = new RecyclableMemoryStreamManager();
    }
}