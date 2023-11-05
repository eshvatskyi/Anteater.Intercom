using FFmpeg.AutoGen.Abstractions;

namespace Anteater.Intercom.Core.ReversChannel;

public unsafe class AudioEncoder : IDisposable
{
    private readonly AVCodecContext* _context;
    private readonly AVFrame* _outFrame;

    private readonly AVFrame* _inFrame;
    private readonly int _inFrameSampleSize;

    private readonly SwrContext* _swrContext;

    private readonly AVPacket* _packet;

    private bool _disposed;

    public AudioEncoder(AVCodecID outCodecId, int outSampleRate, int outChannels, AVSampleFormat inFormat, int inSampleRate, int inChannels)
    {
        var codec = ffmpeg.avcodec_find_encoder(outCodecId);

        _context = ffmpeg.avcodec_alloc_context3(codec);
        _context->sample_fmt = AVSampleFormat.AV_SAMPLE_FMT_S16;
        ffmpeg.av_channel_layout_default(&_context->ch_layout, outChannels);
#pragma warning disable CS0618 // Type or member is obsolete
        _context->channel_layout = _context->ch_layout.u.mask;
        _context->channels = outChannels;
#pragma warning restore CS0618 // Type or member is obsolete
        _context->sample_rate = outSampleRate;

        if (ffmpeg.avcodec_open2(_context, codec, null) < 0)
        {
            throw new Exception("Failed to create encoder.");
        }

        _outFrame = ffmpeg.av_frame_alloc();
        _outFrame->format = (int)_context->sample_fmt;
        ffmpeg.av_channel_layout_copy(&_outFrame->ch_layout, &_context->ch_layout);
#pragma warning disable CS0618 // Type or member is obsolete
        _outFrame->channel_layout = _context->channel_layout;
        _outFrame->channels = _context->channels;
#pragma warning restore CS0618 // Type or member is obsolete
        _outFrame->sample_rate = _context->sample_rate;

        _inFrame = ffmpeg.av_frame_alloc();
        _inFrame->format = (int)inFormat;
        ffmpeg.av_channel_layout_default(&_inFrame->ch_layout, inChannels);
#pragma warning disable CS0618 // Type or member is obsolete
        _inFrame->channel_layout = _inFrame->ch_layout.u.mask;
        _inFrame->channels = inChannels;
#pragma warning restore CS0618 // Type or member is obsolete
        _inFrame->sample_rate = inSampleRate;

        _inFrameSampleSize = ffmpeg.av_get_bytes_per_sample(inFormat);

        fixed (SwrContext** ctx = &_swrContext)
        {
            ffmpeg.swr_alloc_set_opts2(ctx,
                &_outFrame->ch_layout, (AVSampleFormat)_outFrame->format, _outFrame->sample_rate,
                &_inFrame->ch_layout, (AVSampleFormat)_inFrame->format, _inFrame->sample_rate,
                0, null);
        };

        ffmpeg.swr_init(_swrContext);

        _packet = ffmpeg.av_packet_alloc();
    }

    public byte[] Encode(byte[] data)
    {
        try
        {
            var samplesCount = data.Length / (_inFrame->ch_layout.nb_channels * _inFrameSampleSize);

            if (_inFrame->nb_samples != samplesCount)
            {
                _inFrame->nb_samples = samplesCount;

                if (ffmpeg.av_frame_get_buffer(_inFrame, 1) < 0)
                {
                    throw new Exception("Failed to create encoder buffer.");
                }
            }

            fixed (byte* dataPtr = data)
            {
                if (ffmpeg.avcodec_fill_audio_frame(_inFrame, _inFrame->ch_layout.nb_channels, (AVSampleFormat)_inFrame->format, dataPtr, data.Length, 1) < 0)
                {
                    throw new Exception("Failed to fill encoder buffer.");
                }
            }

            if (ffmpeg.swr_convert_frame(_swrContext, _outFrame, _inFrame) < 0)
            {
                throw new Exception("Failed to resample input data.");
            }

            if (ffmpeg.avcodec_send_frame(_context, _outFrame) < 0)
            {
                throw new Exception("Failed to encode buffer.");
            }

            if (ffmpeg.avcodec_receive_packet(_context, _packet) < 0)
            {
                throw new Exception("Failed to receive encoded data.");
            }

            return new Span<byte>(_packet->data, _packet->size).ToArray();
        }
        finally
        {
            ffmpeg.av_packet_unref(_packet);
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                fixed (AVFrame** frame = &_inFrame)
                {
                    ffmpeg.av_frame_free(frame);
                }

                fixed (AVFrame** frame = &_outFrame)
                {
                    ffmpeg.av_frame_free(frame);
                }

                fixed (AVPacket** packet = &_packet)
                {
                    ffmpeg.av_packet_free(packet);
                }

                fixed (AVCodecContext** ctx = &_context)
                {
                    ffmpeg.avcodec_free_context(ctx);
                }
            }

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
