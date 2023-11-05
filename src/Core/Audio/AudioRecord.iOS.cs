using AVFoundation;
using FFmpeg.AutoGen.Abstractions;
using Microsoft.Extensions.Logging;

namespace Anteater.Intercom.Core.Audio;

public partial class AudioRecord
{
    private CancellationTokenSource _workerCancellation;

    private partial void Init()
    {
        var audioSession = AVAudioSession.SharedInstance();

        if (audioSession.RecordPermission == AVAudioSessionRecordPermission.Undetermined)
        {
            audioSession.RequestRecordPermission((_) => { });
        };
    }

    public partial void Start()
    {
        var audioSession = AVAudioSession.SharedInstance();

        if (audioSession.RecordPermission != AVAudioSessionRecordPermission.Granted)
        {
            Stopped?.Invoke();
            return;
        }

        _workerCancellation = new CancellationTokenSource();

        _ = StartRecordingAsync(_workerCancellation.Token).ContinueWith(x =>
        {
            _logger.LogError(x.Exception, "Failed to start recording.");

            Stopped?.Invoke();
        }, TaskContinuationOptions.NotOnRanToCompletion);
    }

    public partial void Stop()
    {
        _workerCancellation?.Cancel();
    }

    async Task StartRecordingAsync(CancellationToken stoppingToken)
    {
        using var engine = new AVAudioEngine();

        using var inputFormat = engine.InputNode.GetBusOutputFormat(0);

        using var format = new AVAudioFormat(inputFormat.CommonFormat, inputFormat.SampleRate, 1, false);

        using var mixer = new AVAudioMixerNode { Volume = 0 };

        engine.InputNode.SetVoiceProcessingEnabled(true, out _);

        engine.AttachNode(mixer);
        engine.Connect(engine.InputNode, mixer, inputFormat);
        engine.Connect(mixer, engine.MainMixerNode, format);
        engine.Prepare();

        mixer.InstallTapOnBus(0, 1024, format, OnDataAvailable);

        if (!engine.StartAndReturnError(out _))
        {
            Stopped?.Invoke();
            return;
        }

        var tcs = new TaskCompletionSource();

        stoppingToken.Register(() => tcs.TrySetResult());

        await tcs.Task;

        mixer.RemoveTapOnBus(0);

        engine.Stop();
    }

    unsafe void OnDataAvailable(AVAudioPcmBuffer pcmBuffer, AVAudioTime when)
    {
        var audioBuffer = pcmBuffer.AudioBufferList[0];
        var data = new ReadOnlySpan<byte>((void*)audioBuffer.Data, audioBuffer.DataByteSize).ToArray();

        DataAvailable?.Invoke(AVSampleFormat.AV_SAMPLE_FMT_FLTP, (int)pcmBuffer.Format.SampleRate, (int)pcmBuffer.Format.ChannelCount, data);
    }
}
