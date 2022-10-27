using System;
using FFmpeg.AutoGen;

namespace Anteater.Intercom.Device.Rtsp;

public unsafe class RtspStreamVideo : RtspStream
{
    private readonly SwsContext* _decodeContext;

    private bool _disposedValue;

    public RtspStreamVideo(AVStream* stream) : base(stream)
    {
        _decodeContext = ffmpeg.sws_getContext(context->width, context->height, context->pix_fmt, context->width, context->height, AVPixelFormat.AV_PIX_FMT_BGRA, 0, null, null, null);
    }

    public override AVMediaType Type { get; } = AVMediaType.AVMEDIA_TYPE_VIDEO;

    public override void DecodeFrame(AVFrame* frame)
    {
        var decodedFrame = ffmpeg.av_frame_alloc();

        if (ffmpeg.av_frame_copy_props(decodedFrame, frame) < 0)
        {
            return;
        }

        decodedFrame->width = frame->width;
        decodedFrame->height = frame->height;
        decodedFrame->format = (int)AVPixelFormat.AV_PIX_FMT_BGRA;

        if (ffmpeg.av_frame_get_buffer(decodedFrame, 0) < 0)
        {
            return;
        }

        if (ffmpeg.sws_scale(_decodeContext, frame->data, frame->linesize, 0, context->height, decodedFrame->data, decodedFrame->linesize) <= 0)
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
