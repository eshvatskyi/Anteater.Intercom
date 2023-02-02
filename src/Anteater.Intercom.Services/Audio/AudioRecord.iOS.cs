using AVFoundation;
using FFmpeg.AutoGen.Abstractions;

namespace Anteater.Intercom.Services.Audio;

public partial class AudioRecord
{
    private AVAudioEngine _engine;
    private AVAudioFormat _format;
    private AVAudioMixerNode _mixer;

    private partial void Init()
    {
        var audioSession = AVAudioSession.SharedInstance();

        audioSession.SetCategory(AVAudioSessionCategory.PlayAndRecord, AVAudioSessionCategoryOptions.DefaultToSpeaker);
        audioSession.SetActive(true);

        _engine = new AVAudioEngine();

        var inputFormat = _engine.InputNode.GetBusOutputFormat(0);

        Format = AVSampleFormat.AV_SAMPLE_FMT_FLTP;
        SampleRate = (int)inputFormat.SampleRate;
        Channels = 1;

        _format = new AVAudioFormat(inputFormat.CommonFormat, SampleRate, (uint)Channels, false);

        _mixer = new AVAudioMixerNode { Volume = 0 };

        _engine.AttachNode(_mixer);

        _engine.Connect(_engine.InputNode, _mixer, inputFormat);

        _engine.Connect(_mixer, _engine.MainMixerNode, _format);

        _engine.Prepare();

        switch (audioSession.RecordPermission)
        {
            case AVAudioSessionRecordPermission.Denied:
                return;

            case AVAudioSessionRecordPermission.Undetermined:

                audioSession.RequestRecordPermission((_) => { });

                return;

        };
    }

    public partial void Start()
    {
        if (AVAudioSession.SharedInstance().RecordPermission != AVAudioSessionRecordPermission.Granted)
        {
            return;
        }

        _mixer.InstallTapOnBus(0, 512, _format, OnDataAvailable);

        _engine.StartAndReturnError(out _);
    }

    public partial void Stop()
    {
        _mixer.RemoveTapOnBus(0);

        _engine.Stop();
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
