namespace Anteater.Intercom.Services.Audio;

public interface IAudioPlayback
{
    bool IsStopped { get; }

    void Init(int sampleRate, int channels);

    void Start();

    void Stop();

    void AddSamples(byte[] data);
}
