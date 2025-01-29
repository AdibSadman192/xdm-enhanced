using System;
using System.Collections.Concurrent;

namespace XDM.Core.IO
{
    /// <summary>
    /// Provides a pool of reusable buffers to reduce memory allocations
    /// </summary>
    public sealed class BufferPool
    {
        private static readonly ConcurrentDictionary<int, ConcurrentQueue<byte[]>> _pools = new();
        private const int MaxPoolSize = 1000;

        /// <summary>
        /// Rents a buffer from the pool
        /// </summary>
        public static byte[] Rent(int minimumSize)
        {
            if (minimumSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(minimumSize));

            // Round up to the next power of 2
            int size = 1;
            while (size < minimumSize) size *= 2;

            if (_pools.TryGetValue(size, out var pool))
            {
                if (pool.TryDequeue(out var buffer))
                {
                    return buffer;
                }
            }

            return new byte[size];
        }

        /// <summary>
        /// Returns a buffer to the pool
        /// </summary>
        public static void Return(byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            var size = buffer.Length;
            var pool = _pools.GetOrAdd(size, _ => new ConcurrentQueue<byte[]>());

            // Only add to pool if we haven't exceeded max size
            if (pool.Count < MaxPoolSize)
            {
                Array.Clear(buffer, 0, buffer.Length);
                pool.Enqueue(buffer);
            }
        }

        /// <summary>
        /// Clears all pooled buffers
        /// </summary>
        public static void Clear()
        {
            _pools.Clear();
        }
    }
}
