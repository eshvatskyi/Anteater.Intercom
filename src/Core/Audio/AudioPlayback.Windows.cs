using NAudio.Wave;

namespace Anteater.Intercom.Core.Audio;

public partial class AudioPlayback : IWaveProvider
{
    private WaveOut _soundOut;

    private partial bool IsStoppedInner()
    {
        return _soundOut.PlaybackState == PlaybackState.Stopped;
    }

    private partial void Init()
    {
        _soundOut = new WaveOut();
    }

    private partial void InnerInit(int sampleRate, int channels)
    {
        WaveFormat = new WaveFormat(sampleRate, 16, channels);
    }

    public partial void Start()
    {
        if (WaveFormat is not null)
        {
            _soundOut.Init(this);
            _soundOut.Play();
        }
    }

    public partial void Stop()
    {
        _soundOut?.Stop();
    }

    public WaveFormat WaveFormat { get; private set; }

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
}
