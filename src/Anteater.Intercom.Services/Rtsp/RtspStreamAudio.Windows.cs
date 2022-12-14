using FFmpeg.AutoGen.Abstractions;

namespace Anteater.Intercom.Services.Rtsp;

public unsafe partial class RtspStreamAudio : RtspStream
{
    private partial int PlatformDecodeFrame(AVFrame* frame)
    {
        if (Format is null)
        {
            Format = new RtspStreamAudioFormat(context->sample_rate, context->ch_layout.nb_channels);
        }

        return ffmpeg.av_samples_get_buffer_size(null, context->ch_layout.nb_channels, frame->nb_samples, context->sample_fmt, 1);
    }
}