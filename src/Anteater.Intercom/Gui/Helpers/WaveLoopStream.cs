using NAudio.Wave;

namespace Anteater.Intercom.Gui.Helpers;

internal class WaveLoopStream : WaveStream
{
    private readonly WaveStream _sourceStream;

    public WaveLoopStream(WaveStream sourceStream)
    {
        _sourceStream = sourceStream;
    }

    public override WaveFormat WaveFormat => _sourceStream.WaveFormat;

    public override long Length => _sourceStream.Length;

    public override long Position
    {
        get => _sourceStream.Position;
        set => _sourceStream.Position = value;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int totalBytesRead = 0;

        while (_sourceStream != null && totalBytesRead < count)
        {
            int bytesRead = _sourceStream.Read(buffer, offset + totalBytesRead, count - totalBytesRead);
            if (bytesRead == 0)
            {
                if (_sourceStream.Position == 0)
                {
                    break;
                }

                _sourceStream.Position = 0;
            }

            totalBytesRead += bytesRead;
        }

        return totalBytesRead;
    }

    protected override void Dispose(bool disposing)
    {
        _sourceStream?.Dispose();

        base.Dispose(disposing);
    }
}
