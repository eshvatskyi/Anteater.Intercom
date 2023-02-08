using FFmpeg.AutoGen.Abstractions;

namespace Anteater.Intercom.Services.Audio;

public partial class AudioRecord : IAudioRecord
{
    public AudioRecord()
    {
        Init();
    }

    public event IAudioRecord.DataAvailableEventHandler DataAvailable;

    public event IAudioRecord.StoppedEventHandler Stopped;

    public AVSampleFormat Format { get; private set; }

    public int SampleRate { get; private set; }

    public int Channels { get; private set; }

    private partial void Init();

    public partial void Start();

    public partial void Stop();
}
