using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FFmpeg.AutoGen;

namespace Anteater.Intercom.Device.Rtsp;

public unsafe class RtspStreamReader : IDisposable
{
    static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(60);

    private readonly List<RtspStream> _streams = new();
    private readonly RtspStreamReaderContext _readerContext = new() { IsStopped = true };
    private readonly object _readLock = new();

    private string _url = null;
    private AVFormatContext* _context = null;
    private Task _workerTask = Task.CompletedTask;
    private bool _disposedValue;

    public bool IsStopped => _readerContext.IsStopped;

    public Action<byte[]> OnVideoFrameDecoded { get; set; }

    public Action<byte[]> OnAudioFrameDecoded { get; set; }

    void Initialize()
    {
        if (string.IsNullOrWhiteSpace(_url))
        {
            return;
        }

        lock (_readLock)
        {
            InitializeContext();
            InitializeStreams();
            InitializeWorker();
        }
    }

    void InitializeContext()
    {
        ffmpeg.avformat_free_context(_context);

        _context = null;

        var context = ffmpeg.avformat_alloc_context();

        context->interrupt_callback.callback = (AVIOInterruptCB_callback)delegate (void* args)
        {
            var ctx = (RtspStreamReaderContext*)args;

            // called from ffmpeg.av_read_frame
            if (ctx->Timestamp == DateTime.MinValue)
            {
                return ctx->IsReadingStopped ? 1 : 0;
            }

            // called from ffmpeg.avformat_open_input
            if ((DateTime.Now - ctx->Timestamp).TotalSeconds > 5)
            {
                return 1;
            }

            return 0;
        };

        fixed (RtspStreamReaderContext* readerContext = &_readerContext)
        {
            readerContext->Timestamp = DateTime.Now;

            context->interrupt_callback.opaque = readerContext;
        }

        if (ffmpeg.avformat_open_input(&context, _url, null, null) < 0)
        {
            ffmpeg.avformat_free_context(context);
            return;
        }

        if (ffmpeg.avformat_find_stream_info(context, null) < 0)
        {
            ffmpeg.avformat_free_context(context);
            return;
        }

        fixed (RtspStreamReaderContext* readerContext = &_readerContext)
        {
            readerContext->Timestamp = DateTime.MinValue;
        }

        _context = context;
    }

    void InitializeStreams()
    {
        _streams.ForEach(x => x.Dispose());
        _streams.Clear();

        if (_context is null)
        {
            return;
        }

        for (var i = 0; i < _context->nb_streams; i++)
        {
            var stream = _context->streams[i];

            _streams.Add(RtspStreamFactory.Create(stream, stream->codecpar->codec_type switch
            {
                AVMediaType.AVMEDIA_TYPE_VIDEO => data => OnVideoFrameDecoded?.Invoke(data),
                AVMediaType.AVMEDIA_TYPE_AUDIO => data => OnAudioFrameDecoded?.Invoke(data),
                _ => null,
            }));
        }
    }

    void InitializeWorker()
    {
        _workerTask.ContinueWith(_ =>
        {
            _workerTask = Task.Run(ReadFrames);
        });
    }

    void ReadFrames()
    {
        fixed (RtspStreamReaderContext* readerContext = &_readerContext)
        {
            readerContext->IsReconnecting = false;
            readerContext->IsStopped = false;
        }

        var packet = ffmpeg.av_packet_alloc();
        var frame = ffmpeg.av_frame_alloc();

        lock (_readLock)
        {
            while (_context is not null && ffmpeg.av_read_frame(_context, packet) >= 0)
            {
                if (_readerContext.IsReadingStopped)
                {
                    break;
                }

                DecodeFrame(packet, frame);

                ffmpeg.av_packet_unref(packet);
                ffmpeg.av_frame_unref(frame);
            }
        }

        ffmpeg.av_packet_free(&packet);
        ffmpeg.av_frame_free(&frame);

        if (!_readerContext.IsStopping)
        {
            Task.Delay(RetryDelay).ContinueWith(_ => Start());
        }

        fixed (RtspStreamReaderContext* readerContext = &_readerContext)
        {
            readerContext->IsStopping = false;
            readerContext->IsStopped = true;
        }
    }

    void DecodeFrame(AVPacket* packet, AVFrame* frame)
    {
        var stream = _streams.ElementAtOrDefault(packet->stream_index);

        if (stream is null)
        {
            return;
        }

        if (ffmpeg.avcodec_send_packet(stream.context, packet) < 0)
        {
            return;
        }

        if (ffmpeg.avcodec_receive_frame(stream.context, frame) < 0)
        {
            return;
        }

        stream.DecodeFrame(frame);
    }

    public RtspStreamFormat Start(string url)
    {
        _url = url;

        Initialize();

        var vStream = _streams.FirstOrDefault(x => x.Type == AVMediaType.AVMEDIA_TYPE_VIDEO);
        var aStream = _streams.FirstOrDefault(x => x.Type == AVMediaType.AVMEDIA_TYPE_AUDIO);

        return new RtspStreamFormat(
            Video: vStream is null ? null : new RtspStreamVideoFormat(vStream.context->width, vStream.context->height, vStream.context->pix_fmt),
            Audio: aStream is null ? null : new RtspStreamAudioFormat(aStream.context->sample_rate, aStream.context->ch_layout.nb_channels));
    }

    public void Start()
    {
        fixed (RtspStreamReaderContext* readerContext = &_readerContext)
        {
            readerContext->IsReconnecting = true;
        }

        Initialize();
    }

    public void Stop()
    {
        fixed (RtspStreamReaderContext* readerContext = &_readerContext)
        {
            readerContext->IsStopping = true;
        }

        lock (_readLock)
        {
            if (_context is not null)
            {
                ffmpeg.av_read_pause(_context);
                ffmpeg.avformat_free_context(_context);
                _context = null;
            }

            _streams.ForEach(x => x.Dispose());
            _streams.Clear();
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                OnVideoFrameDecoded = null;
                OnAudioFrameDecoded = null;

                Stop();
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
