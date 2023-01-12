using System.Runtime.InteropServices;
using AVFoundation;

namespace Anteater.Intercom.Services.Audio;

public partial class AudioPlayback
{
    private readonly byte[] _samplesBuffer = new byte[1024];

    private AVAudioEngine _engine;
    private AVAudioPlayerNode _player;
    private AVAudioPcmBuffer _audioBuffer;

    private partial bool IsStoppedInner()
    {
        return _engine.Running == false;
    }

    private partial void Init()
    {
        var audioSession = AVAudioSession.SharedInstance();

        audioSession.SetCategory(AVAudioSessionCategory.PlayAndRecord, AVAudioSessionCategoryOptions.DefaultToSpeaker);
        audioSession.SetActive(true);

        _engine = new AVAudioEngine();

        _player = new AVAudioPlayerNode();
        _player.Volume = 1;

        _engine.AttachNode(_player);
    }

    private partial void InnerInit(int sampleRate, int channels)
    {
        _engine.Stop();
        _engine.Reset();

        var format = new AVAudioFormat(sampleRate, (uint)channels);

        _audioBuffer = new AVAudioPcmBuffer(format, (uint)_samplesBuffer.Length / 4);

        _engine.Connect(_player, _engine.MainMixerNode, format);
        _engine.Prepare();
    }

    public partial void Start()
    {
        _engine.StartAndReturnError(out _);

        _player.Play();

        ReadSamples();
    }

    public partial void Stop()
    {
        _player.Stop();

        _engine.Stop();
    }

    void ReadSamples()
    {
        var num = _buffer.Read(_samplesBuffer, 0, _samplesBuffer.Length);

        if (num < _samplesBuffer.Length)
        {
            _samplesBuffer.AsSpan()[num..].Clear();
        }

        unsafe
        {
            var channels = (nint*)_audioBuffer.FloatChannelData.ToPointer();

            Marshal.Copy(_samplesBuffer, 0, channels[0], _samplesBuffer.Length);

            _audioBuffer.FrameLength = _audioBuffer.FrameCapacity;
        }

        _player.ScheduleBuffer(_audioBuffer, ReadSamples);
    }
}
