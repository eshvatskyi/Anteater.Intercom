using System;
using FFmpeg.AutoGen;

namespace Anteater.Intercom.Device.Rtsp;

public unsafe abstract class RtspStream : IDisposable
{
    public delegate void FrameDecodedEventHandler(object sender, byte[] data);

    public readonly AVCodec* codec;
    public readonly AVCodecContext* context;

    public event FrameDecodedEventHandler FrameDecoded;

    private bool _disposedValue;

    public RtspStream(AVStream* stream)
    {
        if (stream is not null)
        {
            codec = ffmpeg.avcodec_find_decoder(stream->codecpar->codec_id);
            context = ffmpeg.avcodec_alloc_context3(codec);

            ffmpeg.avcodec_parameters_to_context(context, stream->codecpar);
            ffmpeg.avcodec_open2(context, codec, null);
        }
    }

    public abstract AVMediaType Type { get; }

    public abstract void DecodeFrame(AVFrame* frame);

    protected void OnFrameDecoded(byte[] data)
    {
        FrameDecoded?.Invoke(this, data);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                fixed (AVCodecContext** ctx = &context)
                {
                    ffmpeg.avcodec_free_context(ctx);
                }

                ffmpeg.av_free(codec);
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
