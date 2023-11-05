using FFmpeg.AutoGen.Abstractions;
using NAudio.Wave;

namespace Anteater.Intercom.Core.Audio;

public partial class AudioRecord
{
    private WaveInEvent _soundIn;

    private partial void Init() { }

    public partial void Start()
    {
        _soundIn = new WaveInEvent();
        _soundIn.WaveFormat = new WaveFormat(44100, 16, 1);
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
        DataAvailable?.Invoke(AVSampleFormat.AV_SAMPLE_FMT_S16, 44100, 1, e.Buffer);
    }

    void OnStopped(object sender, StoppedEventArgs e)
    {
        _ = Task.Run(() => Stopped?.Invoke());
    }
}
