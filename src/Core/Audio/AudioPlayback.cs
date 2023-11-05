namespace Anteater.Intercom.Core.Audio;

public partial class AudioPlayback : IAudioPlayback
{
    private QueuedBuffer _buffer;

    public AudioPlayback()
    {
        Init();
    }

    public bool IsStopped => IsStoppedInner();

    private partial bool IsStoppedInner();

    private partial void Init();

    public void Init(int sampleRate, int channels)
    {
        _buffer = new QueuedBuffer(sampleRate / 2);

        InnerInit(sampleRate, channels);
    }

    private partial void InnerInit(int sampleRate, int channels);

    public partial void Start();

    public partial void Stop();

    public void AddSamples(byte[] data)
    {
        _buffer?.Write(data, 0, data.Length);
    }
}
