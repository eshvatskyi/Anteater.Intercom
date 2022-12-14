using FFmpeg.AutoGen.Abstractions;

namespace Anteater.Intercom.Services.Rtsp;

/// <summary>
///  AVAudioEngine can only play 32-bit non-interleaved (planar) floating point samples.
/// </summary>
public unsafe partial class RtspStreamAudio : RtspStream
{
    private SwrContext* _swrContext;

    private bool _disposedValue;

    private partial int PlatformDecodeFrame(AVFrame* frame)
    {
        var outFrame = ffmpeg.av_frame_alloc();

        ffmpeg.av_channel_layout_copy(&outFrame->ch_layout, &frame->ch_layout);

        outFrame->sample_rate = frame->sample_rate;
        outFrame->format = (int)AVSampleFormat.AV_SAMPLE_FMT_FLTP;

        if (_swrContext is null)
        {
            Format = new RtspStreamAudioFormat(context->sample_rate, context->ch_layout.nb_channels);

            fixed (SwrContext** ctx = &_swrContext)
            {
                ffmpeg.swr_alloc_set_opts2(ctx,
                    &outFrame->ch_layout, (AVSampleFormat)outFrame->format, outFrame->sample_rate,
                    &frame->ch_layout, (AVSampleFormat)frame->format, frame->sample_rate,
                    0, null);
            };

            ffmpeg.swr_init(_swrContext);
        }

        if (ffmpeg.swr_convert_frame(_swrContext, outFrame, frame) < 0)
        {
            return 0;
        }

        ffmpeg.av_frame_unref(frame);
        ffmpeg.av_frame_move_ref(frame, outFrame);

        return ffmpeg.av_samples_get_buffer_size(null, context->ch_layout.nb_channels, frame->nb_samples, AVSampleFormat.AV_SAMPLE_FMT_FLTP, 1);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!_disposedValue)
        {
            if (disposing)
            {
                if (_swrContext is not null)
                {
                    fixed (SwrContext** context = &_swrContext)
                    {
                        ffmpeg.swr_free(context);
                    }
                }
            }

            _disposedValue = true;
        }
    }
}
