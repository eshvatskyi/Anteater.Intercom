using System.Runtime.InteropServices;
using AVFoundation;

namespace Anteater.Intercom.Services.Audio;

public partial class AudioPlayback
{
    private readonly byte[] _samplesBuffer = new byte[1024];

    private AVAudioEngine _engine;
    private AVAudioPlayerNode _player;
    private AVAudioPcmBuffer _audioBuffer;

    private bool _initialized = false;
    private bool _running = false;
    private Task _lastTask = Task.CompletedTask;

    private partial bool IsStoppedInner()
    {
        return _running == false;
    }

    private partial void Init()
    {
        _lastTask = _lastTask.ContinueWith(x =>
        {
            var audioSession = AVAudioSession.SharedInstance();

            audioSession.SetCategory(AVAudioSessionCategory.PlayAndRecord, AVAudioSessionCategoryOptions.DefaultToSpeaker);
            audioSession.SetActive(true);

            _engine = new AVAudioEngine();

            _player = new AVAudioPlayerNode();
            _player.Volume = 1;

            _engine.AttachNode(_player);
        });
    }

    private partial void InnerInit(int sampleRate, int channels)
    {
        _lastTask = _lastTask.ContinueWith(x =>
        {
            _engine.Stop();
            _engine.Reset();

            var format = new AVAudioFormat(sampleRate, (uint)channels);

            _audioBuffer = new AVAudioPcmBuffer(format, (uint)_samplesBuffer.Length / 4);

            _engine.Connect(_player, _engine.MainMixerNode, format);
            _engine.Prepare();

            _initialized = true;
        });
    }

    public partial void Start()
    {
        _lastTask = _lastTask.ContinueWith(x =>
        {
            if (!_initialized)
            {
                return;
            }

            _ = Task.Run(() =>
            {
                _running = true;

                _engine.StartAndReturnError(out _);

                _player.Play();

                ReadSamples();                
            });
        });
    }

    public partial void Stop()
    {
        _lastTask = _lastTask.ContinueWith(x =>
        {
            _running = false;

            _player.Stop();
        });
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
