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

        _soundIn = new WaveIn(new WaveFormat(SampleRate, 16, Channels));        
    }

    public partial void Start()
    {
        _soundIn.Initialize();

        _soundIn.DataAvailable += OnDataAvailable;

        _soundIn.Start();
    }

    public partial void Stop()
    {
        _soundIn.DataAvailable -= OnDataAvailable;

        _soundIn.Stop();
    }

    void OnDataAvailable(object sender, DataAvailableEventArgs args)
    {
        DataAvailable?.Invoke(args.Data);
    }
}
