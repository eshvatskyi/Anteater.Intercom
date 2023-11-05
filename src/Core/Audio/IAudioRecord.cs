using FFmpeg.AutoGen.Abstractions;

namespace Anteater.Intercom.Core.Audio;

public interface IAudioRecord
{
    public delegate void DataAvailableEventHandler(AVSampleFormat format, int sampleRate, int channels, byte[] data);

    public delegate void StoppedEventHandler();

    event DataAvailableEventHandler DataAvailable;

    event StoppedEventHandler Stopped;

    void Start();

    void Stop();
}
