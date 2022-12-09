using System;
using FFmpeg.AutoGen.Abstractions;

namespace Anteater.Intercom.Services.Rtsp;

public unsafe abstract class RtspStream : IDisposable
{
    public delegate void FrameDecodedEventHandler(RtspStream stream, byte[] data);

    public readonly AVCodecContext* context;

    private bool _disposedValue;

    public RtspStream(AVStream* stream)
    {
        if (stream is not null)
        {
            var codec = ffmpeg.avcodec_find_decoder(stream->codecpar->codec_id);

            context = ffmpeg.avcodec_alloc_context3(codec);

            ffmpeg.avcodec_parameters_to_context(context, stream->codecpar);
            ffmpeg.avcodec_open2(context, codec, null);
        }
    }

    public event FrameDecodedEventHandler FrameDecoded;

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
                if (context is not null)
                {
                    fixed (AVCodecContext** ctx = &context)
                    {
                        ffmpeg.avcodec_free_context(ctx);
                    }
                }
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
