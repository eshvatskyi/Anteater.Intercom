using CSCore;
using CSCore.SoundOut;

namespace Anteater.Intercom.Services.Audio;

public partial class AudioPlayback : IWaveSource
{
    private WasapiOut _soundOut;

    private partial bool IsStoppedInner()
    {
        return _soundOut.PlaybackState == PlaybackState.Stopped;
    }

    private partial void Init()
    {
        _soundOut = new WasapiOut();
    }

    private partial void InnerInit(int sampleRate, int channels)
    {
        WaveFormat = new WaveFormat(sampleRate, 16, channels);        
    }

    public partial void Start()
    {
        if (WaveFormat is not null)
        {
            _soundOut.Initialize(this);
            _soundOut.Play();
        }        
    }

    public partial void Stop()
    {
        _soundOut?.Stop();
    }

    public bool CanSeek { get; } = false;

    public WaveFormat WaveFormat { get; private set; }

    public long Position { get; set; }

    public long Length => _buffer.Length;

    public int Read(byte[] buffer, int offset, int count)
    {
        var num = _buffer.Read(buffer, offset, count);

        if (num < count)
        {
            buffer.AsSpan().Slice(offset + num, count - num).Clear();

            num = count;
        }

        return num;
    }

    public void Dispose() { }
}
