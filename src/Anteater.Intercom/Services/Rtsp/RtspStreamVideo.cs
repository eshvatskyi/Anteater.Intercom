using System;
using FFmpeg.AutoGen.Abstractions;

namespace Anteater.Intercom.Services.Rtsp;

public unsafe class RtspStreamVideo : RtspStream
{
    private SwsContext* _decodeContext;

    private bool _disposedValue;

    public RtspStreamVideo(AVStream* stream) : base(stream) { }

    public override AVMediaType Type { get; } = AVMediaType.AVMEDIA_TYPE_VIDEO;

    public RtspStreamVideoFormat Format { get; private set; }

    public override void DecodeFrame(AVFrame* frame)
    {
        var decodedFrame = ffmpeg.av_frame_alloc();

        decodedFrame->width = frame->width;
        decodedFrame->height = frame->height;
        decodedFrame->format = (int)AVPixelFormat.AV_PIX_FMT_BGRA;

        if (_decodeContext is null)
        {
            Format = new RtspStreamVideoFormat(decodedFrame->width, decodedFrame->height, (AVPixelFormat)decodedFrame->format);

            _decodeContext = ffmpeg.sws_getContext(frame->width, frame->height, (AVPixelFormat)frame->format, decodedFrame->width, decodedFrame->height, (AVPixelFormat)decodedFrame->format, 0, null, null, null);
        }

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
                if (_decodeContext is not null)
                {
                    ffmpeg.sws_freeContext(_decodeContext);
                }
            }

            _disposedValue = true;
        }
    }
}
