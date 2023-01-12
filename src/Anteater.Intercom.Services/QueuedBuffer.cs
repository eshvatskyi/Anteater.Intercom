namespace Anteater.Intercom.Services;

public class QueuedBuffer
{
    private readonly object _lock = new();

    private byte[] _buffer;

    private int _bufferSize;

    public QueuedBuffer(int size)
    {
        _buffer = new byte[size];
    }

    public int Length => _bufferSize;

    public int Write(byte[] data, int offset, int count)
    {
        lock (_lock)
        {
            if (_buffer.Length < count)
            {
                Array.Resize(ref _buffer, count);
            }

            var freeCapacity = _buffer.Length - _bufferSize;

            if (count > freeCapacity)
            {
                var needCapacity = count - freeCapacity;

                Buffer.BlockCopy(_buffer, needCapacity, _buffer, 0, _bufferSize - needCapacity);

                _bufferSize -= needCapacity;
            }

            Buffer.BlockCopy(data, offset, _buffer, _bufferSize, count);

            _bufferSize += count;

            return count;
        }
    }

    public int Read(byte[] data, int offset, int count)
    {
        lock (_lock)
        {
            if (count > _bufferSize)
            {
                count = _bufferSize;
            }

            Buffer.BlockCopy(_buffer, 0, data, offset, count);

            Buffer.BlockCopy(_buffer, count, _buffer, 0, _bufferSize - count);

            _bufferSize -= count;

            return count;
        }
    }

    public int Read(Span<byte> data, int offset, int count)
    {
        lock (_lock)
        {
            if (count > _bufferSize)
            {
                count = _bufferSize;
            }

            _buffer.AsSpan()[..count].CopyTo(data.Slice(offset, count));

            Buffer.BlockCopy(_buffer, count, _buffer, 0, _bufferSize - count);

            _bufferSize -= count;

            return count;
        }
    }
}
