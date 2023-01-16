using FFmpeg.AutoGen.Abstractions;

namespace Anteater.Intercom.Services.Audio;

public interface IAudioRecord
{
    public delegate void DataAvailableEventHandler(byte[] data);

    public delegate void StoppedEventHandler();

    event DataAvailableEventHandler DataAvailable;

    event StoppedEventHandler Stopped;

    AVSampleFormat Format { get; }

    int SampleRate { get; }

    int Channels { get; }

    void Start();

    void Stop();
}
