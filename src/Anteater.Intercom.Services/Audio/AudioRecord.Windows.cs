using CSCore;
using CSCore.SoundIn;
using FFmpeg.AutoGen.Abstractions;

namespace Anteater.Intercom.Services.Audio;

public partial class AudioRecord
{
    private WaveIn _soundIn;

    private partial void Init()
    {
        Format = AVSampleFormat.AV_SAMPLE_FMT_S16;
        SampleRate = 44100;
        Channels = 1;
    }

    public partial void Start()
    {
        _soundIn = new WaveIn(new WaveFormat(SampleRate, 16, Channels));

        _soundIn.Initialize();

        _soundIn.DataAvailable += OnDataAvailable;
        _soundIn.Stopped += OnStopped;

        _soundIn.Start();
    }

    public partial void Stop()
    {
        _soundIn.DataAvailable -= OnDataAvailable;
        _soundIn.Stopped -= OnStopped;

        _soundIn.Stop();

        try
        {
            _soundIn.Dispose();
        }
        catch { }

        Stopped?.Invoke();
    }

    void OnDataAvailable(object sender, DataAvailableEventArgs e)
    {
        DataAvailable?.Invoke(e.Data);
    }

    void OnStopped(object sender, RecordingStoppedEventArgs e)
    {
        _ = Task.Run(() => Stopped?.Invoke());
    }
}
