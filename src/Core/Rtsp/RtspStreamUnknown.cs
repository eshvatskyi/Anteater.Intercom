using FFmpeg.AutoGen.Abstractions;

namespace Anteater.Intercom.Core.Rtsp;

public unsafe class RtspStreamUnknown : RtspStream
{
    public RtspStreamUnknown() : base(null) { }

    public override AVMediaType Type { get; } = AVMediaType.AVMEDIA_TYPE_UNKNOWN;

    public override void DecodeFrame(AVFrame* frame) { }
}
