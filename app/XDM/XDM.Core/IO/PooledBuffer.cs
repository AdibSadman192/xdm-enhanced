using System;
using System.Buffers;

namespace XDM.Core.IO
{
    public class PooledBuffer : IDisposable
    {
        private readonly ArrayPool<byte> _pool;
        private bool _disposed;

        public byte[] Buffer { get; private set; }

        public PooledBuffer(int size)
        {
            _pool = ArrayPool<byte>.Shared;
            Buffer = _pool.Rent(size);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (Buffer != null)
                {
                    _pool.Return(Buffer);
                    Buffer = null;
                }
                _disposed = true;
            }
        }
    }
}
