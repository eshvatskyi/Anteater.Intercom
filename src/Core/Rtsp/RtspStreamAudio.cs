using FFmpeg.AutoGen.Abstractions;

namespace Anteater.Intercom.Core.Rtsp;

public unsafe partial class RtspStreamAudio : RtspStream
{
    public RtspStreamAudio(AVStream* stream) : base(stream) { }

    public override AVMediaType Type { get; } = AVMediaType.AVMEDIA_TYPE_AUDIO;

    public RtspStreamAudioFormat Format { get; private set; }

    private partial int PlatformDecodeFrame(AVFrame* frame);

    public override void DecodeFrame(AVFrame* frame)
    {
        var size = PlatformDecodeFrame(frame);

        if (size < 0)
        {
            return;
        }

        var data = new Span<byte>(frame->data[0], size);

        OnFrameDecoded(data.ToArray());
    }
}
