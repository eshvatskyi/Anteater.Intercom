using System;
using FFmpeg.AutoGen;

namespace Anteater.Intercom.Device.Rtsp;

public unsafe class RtspStreamAudio : RtspStream
{
    public RtspStreamAudio(AVStream* stream) : base(stream) { }

    public override AVMediaType Type { get; } = AVMediaType.AVMEDIA_TYPE_AUDIO;

    public override void DecodeFrame(AVFrame* frame)
    {
        var size = ffmpeg.av_samples_get_buffer_size(null, context->ch_layout.nb_channels, frame->nb_samples, context->sample_fmt, 1);

        if (size > 0)
        {
            var data = new Span<byte>(frame->data[0], size);

            OnFrameDecoded(data.ToArray());
        }
    }
}
