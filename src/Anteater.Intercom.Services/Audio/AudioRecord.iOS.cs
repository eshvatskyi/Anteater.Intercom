using AVFoundation;
using FFmpeg.AutoGen.Abstractions;

namespace Anteater.Intercom.Services.Audio;

public partial class AudioRecord
{
    private CancellationTokenSource _workerCancellation;

    private partial void Init()
    {
        var audioSession = AVAudioSession.SharedInstance();

        audioSession.SetCategory(AVAudioSessionCategory.PlayAndRecord, AVAudioSessionCategoryOptions.DefaultToSpeaker);
        audioSession.SetActive(true);

        switch (audioSession.RecordPermission)
        {
            case AVAudioSessionRecordPermission.Denied:
                return;

            case AVAudioSessionRecordPermission.Undetermined:

                audioSession.RequestRecordPermission((_) => { });

                return;

        };

        Format = AVSampleFormat.AV_SAMPLE_FMT_FLTP;
        SampleRate = 44100;
        Channels = 1;
    }

    public partial void Start()
    {
        if (AVAudioSession.SharedInstance().RecordPermission != AVAudioSessionRecordPermission.Granted)
        {
            return;
        }

        _workerCancellation = new CancellationTokenSource();

        _ = StartRecordingAsync(_workerCancellation.Token);
    }

    public partial void Stop()
    {
        _workerCancellation?.Cancel();
    }

    async Task StartRecordingAsync(CancellationToken stoppingToken)
    {
        var engine = new AVAudioEngine();

        var inputFormat = engine.InputNode.GetBusOutputFormat(0);

        var format = new AVAudioFormat(inputFormat.CommonFormat, SampleRate, (uint)Channels, false);

        var mixer = new AVAudioMixerNode { Volume = 0 };

        engine.AttachNode(mixer);

        engine.Connect(engine.InputNode, mixer, inputFormat);

        engine.Connect(mixer, engine.MainMixerNode, format);

        engine.Prepare();

        mixer.InstallTapOnBus(0, 512, format, OnDataAvailable);

        engine.StartAndReturnError(out _);

        var tcs = new TaskCompletionSource();

        stoppingToken.Register(() => tcs.TrySetResult());

        await tcs.Task;

        mixer.RemoveTapOnBus(0);

        engine.Stop();
    }

    void OnDataAvailable(AVAudioPcmBuffer buffer, AVAudioTime when)
    {
        unsafe
        {
            var audioBuffer = buffer.AudioBufferList[0];

            var data = new Span<byte>((void*)audioBuffer.Data, audioBuffer.DataByteSize);

            DataAvailable?.Invoke(data.ToArray());
        }
    }
}
