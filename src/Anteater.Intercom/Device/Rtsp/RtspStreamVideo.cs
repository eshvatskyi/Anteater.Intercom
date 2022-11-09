using System;
using FFmpeg.AutoGen;

namespace Anteater.Intercom.Device.Rtsp;

public unsafe class RtspStreamVideo : RtspStream
{
    private readonly SwsContext* _decodeContext;

    private bool _disposedValue;

    public RtspStreamVideo(AVStream* stream) : base(stream)
    {
        if (context is not null)
        {
            _decodeContext = ffmpeg.sws_getContext(context->width, context->height, context->pix_fmt, context->width, context->height, AVPixelFormat.AV_PIX_FMT_BGRA, 0, null, null, null);
        }
    }

    public override AVMediaType Type { get; } = AVMediaType.AVMEDIA_TYPE_VIDEO;

    public override void DecodeFrame(AVFrame* frame)
    {
        var decodedFrame = ffmpeg.av_frame_alloc();

        decodedFrame->width = frame->width;
        decodedFrame->height = frame->height;
        decodedFrame->format = (int)AVPixelFormat.AV_PIX_FMT_BGRA;

        if (ffmpeg.sws_scale_frame(_decodeContext, decodedFrame, frame) <= 0)
        {
            return;
        }

        ffmpeg.av_frame_unref(frame);
        ffmpeg.av_frame_move_ref(frame, decodedFrame);

        var size = ffmpeg.av_image_get_buffer_size(AVPixelFormat.AV_PIX_FMT_BGRA, frame->width, frame->height, 1);

        var data = new Span<byte>(frame->data[0], size);

        OnFrameDecoded(data.ToArray());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!_disposedValue)
        {
            if (disposing)
            {
                ffmpeg.sws_freeContext(_decodeContext);
            }

            _disposedValue = true;
        }
    }
}
