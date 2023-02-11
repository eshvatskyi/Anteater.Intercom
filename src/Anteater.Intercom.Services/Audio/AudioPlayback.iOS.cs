using AVFoundation;

namespace Anteater.Intercom.Services.Audio;

public partial class AudioPlayback
{
    private readonly byte[] _samplesBuffer = new byte[1024];

    private AVAudioFormat _format;
    private bool _isRunning = true;
    private CancellationTokenSource _workerCancellation;

    private partial bool IsStoppedInner()
    {
        return _isRunning == false;
    }

    private partial void Init() { }

    private partial void InnerInit(int sampleRate, int channels)
    {
        _format = new AVAudioFormat(sampleRate, (uint)channels);
    }

    public partial void Start()
    {
        if (_format is null)
        {
            return;
        }

        _workerCancellation = new CancellationTokenSource();

        _ = StartPlaybackAsync(_workerCancellation.Token);
    }

    public partial void Stop()
    {
        _workerCancellation?.Cancel();
    }

    async Task StartPlaybackAsync(CancellationToken stoppingToken)
    {
        _isRunning = true;

        var audioSession = AVAudioSession.SharedInstance();

        var audioSessionOpts = AVAudioSessionCategoryOptions.DefaultToSpeaker
            | AVAudioSessionCategoryOptions.OverrideMutedMicrophoneInterruption
            | AVAudioSessionCategoryOptions.AllowBluetooth
            | AVAudioSessionCategoryOptions.AllowBluetoothA2DP;

        audioSession.SetCategory(AVAudioSessionCategory.PlayAndRecord, audioSessionOpts);
        audioSession.SetMode((NSString)"videoChat", out _);
        audioSession.SetActive(true);

        var engine = new AVAudioEngine();

        engine.OutputNode.SetVoiceProcessingEnabled(true, out _);

        var player = new AVAudioPlayerNode { Volume = 1 };

        engine.AttachNode(player);
        engine.Connect(player, engine.MainMixerNode, _format);
        engine.Prepare();
        engine.StartAndReturnError(out _);

        player.Play();

        var pcmBuffer = new AVAudioPcmBuffer(_format, (uint)_samplesBuffer.Length / 4);

        ReadSamples(pcmBuffer, player);

        var tcs = new TaskCompletionSource();

        stoppingToken.Register(() => tcs.TrySetResult());

        await tcs.Task;

        player.Stop();

        engine.Stop();

        audioSession.SetActive(false);

        _isRunning = false;
    }

    void ReadSamples(AVAudioPcmBuffer pcmBuffer, AVAudioPlayerNode player)
    {
        if (_workerCancellation.IsCancellationRequested)
        {
            return;
        }

        var num = _buffer.Read(_samplesBuffer, 0, _samplesBuffer.Length);

        if (num < _samplesBuffer.Length)
        {
            _samplesBuffer.AsSpan()[num..].Clear();
        }

        unsafe
        {
            var channels = (nint*)pcmBuffer.FloatChannelData.ToPointer();

            _samplesBuffer.AsSpan().CopyTo(new Span<byte>((void*)channels[0], _samplesBuffer.Length));

            pcmBuffer.FrameLength = pcmBuffer.FrameCapacity;
        }

        player.ScheduleBuffer(pcmBuffer, () => ReadSamples(pcmBuffer, player));
    }
}
