using FFmpeg.AutoGen.Abstractions;
using NAudio.Wave;

namespace Anteater.Intercom.Services.Audio;

public partial class AudioRecord
{
    private WaveInEvent _soundIn;

    private partial void Init()
    {
        Format = AVSampleFormat.AV_SAMPLE_FMT_S16;
        SampleRate = 44100;
        Channels = 1;
    }

    public partial void Start()
    {
        _soundIn = new WaveInEvent();
        _soundIn.WaveFormat = new WaveFormat(SampleRate, 16, Channels);
        _soundIn.DataAvailable += OnDataAvailable;
        _soundIn.RecordingStopped += OnStopped;

        _soundIn.StartRecording();
    }

    public partial void Stop()
    {
        using (_soundIn)
        {
            _soundIn.DataAvailable -= OnDataAvailable;
            _soundIn.RecordingStopped -= OnStopped;
            _soundIn.StopRecording();
        }
        
        Stopped?.Invoke();
    }

    void OnDataAvailable(object sender, WaveInEventArgs e)
    {
        DataAvailable?.Invoke(e.Buffer);
    }

    void OnStopped(object sender, StoppedEventArgs e)
    {
        _ = Task.Run(() => Stopped?.Invoke());
    }
}
