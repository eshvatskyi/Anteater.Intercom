using System.Net;
using System.Net.Sockets;
using FFmpeg.AutoGen.Abstractions;
using static Anteater.Intercom.Services.Rtsp.RtspStream;

namespace Anteater.Intercom.Services.Rtsp;

public unsafe class RtspStreamReader : IDisposable
{
    static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(60);

    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private readonly List<RtspStream> _streams = new();

    private string _url = null;
    private bool _udpSupport = false;
    private AVFormatContext* _context = null;
    private Task _workerTask = Task.CompletedTask;
    private CancellationTokenSource _cts;
    private bool _disposedValue;

    public bool IsReadingStopped => IsReconnecting || IsStopping || IsStopped;

    public bool IsReconnecting { get; private set; }

    public bool IsStopping { get; private set; }

    public bool IsStopped { get; private set; } = true;

    public event FrameDecodedEventHandler VideoFrameDecoded;

    public event FrameDecodedEventHandler AudioFrameDecoded;

    void Initialize()
    {
        if (string.IsNullOrWhiteSpace(_url))
        {
            return;
        }

        Stop();

        _semaphore.Wait();

        try
        {
            IsStopped = false;

            InitializeContext();
            InitializeStreams();
            InitializeWorker();
        }
        finally
        {
            _semaphore.Release();
        }

        // Sometimes (on re-connect) video stream not properly initialized.
        // We need to read a frame to get proper format
        var vStream = _streams.OfType<RtspStreamVideo>().FirstOrDefault();

        while (!IsReadingStopped && vStream is not null && vStream.Format is null)
        {
            Thread.Sleep(5);
        }
    }

    bool CanInitializeContext()
    {
        var timestamp = DateTime.Now;

        var context = ffmpeg.avformat_alloc_context();

        context->interrupt_callback = new AVIOInterruptCB
        {
            callback = (AVIOInterruptCB_callback)delegate (void* args)
            {
                if ((DateTime.Now - *(DateTime*)args).TotalSeconds > 10)
                {
                    return 1;
                }

                return 0;
            },
            opaque = &timestamp,
        };

        AVDictionary* stream_opts;

        ffmpeg.av_dict_set(&stream_opts, "rtsp_transport", "tcp", 0);
        ffmpeg.av_dict_set(&stream_opts, "rtpflags", "send_bye", 0);

        var result = ffmpeg.avformat_open_input(&context, _url, null, &stream_opts) >= 0;

        CloseContext(context);

        return result;
    }

    void InitializeContext()
    {
        //ffmpeg.av_log_set_level(ffmpeg.AV_LOG_DEBUG);
        //ffmpeg.av_log_set_callback((av_log_set_callback_callback)delegate (void* p0, int level, string format, byte* vl)
        //{
        //    var lineSize = 1024;
        //    var lineBuffer = stackalloc byte[lineSize];
        //    var printPrefix = 1;

        //    ffmpeg.av_log_format_line(p0, level, format, vl, lineBuffer, lineSize, &printPrefix);

        //    Debug.Write(Marshal.PtrToStringAnsi((IntPtr)lineBuffer));
        //});

        if (!CanInitializeContext())
        {
            return;
        }

        var context = ffmpeg.avformat_alloc_context();

        AVDictionary* stream_opts;

        ffmpeg.av_dict_set(&stream_opts, "rtsp_transport", _udpSupport ? "udp" : "tcp", 0);
        ffmpeg.av_dict_set(&stream_opts, "timeout", $"{1 * 1000 * 1000}", 0);
        ffmpeg.av_dict_set(&stream_opts, "rtpflags", "send_bye", 0);

        if (ffmpeg.avformat_open_input(&context, _url, null, &stream_opts) < 0)
        {
            CloseContext(context);
            return;
        }

        if (ffmpeg.avformat_find_stream_info(context, null) < 0)
        {
            CloseContext(context);
            return;
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
            AddStream(_context->streams[i]);
        }
    }

    void AddStream(AVStream* stream)
    {
        RtspStream rtspStream = stream->codecpar->codec_type switch
        {
            AVMediaType.AVMEDIA_TYPE_VIDEO => new RtspStreamVideo(stream),
            AVMediaType.AVMEDIA_TYPE_AUDIO => new RtspStreamAudio(stream),
            _ => new RtspStreamUnknown(),
        };

        rtspStream.FrameDecoded += stream->codecpar->codec_type switch
        {
            AVMediaType.AVMEDIA_TYPE_VIDEO => (stream, data) => VideoFrameDecoded?.Invoke(stream, data),
            AVMediaType.AVMEDIA_TYPE_AUDIO => (stream, data) => AudioFrameDecoded?.Invoke(stream, data),
            _ => null,
        };

        _streams.Add(rtspStream);
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
        IsReconnecting = false;
        IsStopped = false;

        var packet = ffmpeg.av_packet_alloc();
        var frame = ffmpeg.av_frame_alloc();

        _semaphore.Wait();

        try
        {
            while (_context is not null && ffmpeg.av_read_frame(_context, packet) >= 0)
            {
                if (IsReadingStopped)
                {
                    break;
                }

                DecodeFrame(packet, frame);

                ffmpeg.av_packet_unref(packet);
                ffmpeg.av_frame_unref(frame);
            }
        }
        finally
        {
            _semaphore.Release();
        }

        ffmpeg.av_packet_free(&packet);
        ffmpeg.av_frame_free(&frame);

        if (!IsStopping)
        {
            _cts = new CancellationTokenSource();

            Task.Delay(RetryDelay, _cts.Token).ContinueWith(_ => Start(), TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        IsStopping = false;
        IsStopped = true;
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
        var uri = new Uri(url);

        if (uri.HostNameType == UriHostNameType.IPv4)
        {
            _udpSupport = IPAddress.Parse(uri.Host).AddressFamily == AddressFamily.InterNetwork;
        }

        _url = uri.ToString();

        Initialize();

        var vStream = _streams.OfType<RtspStreamVideo>().FirstOrDefault();
        var aStream = _streams.OfType<RtspStreamAudio>().FirstOrDefault();

        return new RtspStreamFormat(Video: vStream?.Format, Audio: aStream?.Format);
    }

    public void Start()
    {
        IsReconnecting = true;

        Initialize();
    }

    public void Stop()
    {
        _cts?.Cancel();

        IsStopping = !IsStopped;

        _semaphore.Wait();

        try
        {
            CloseContext(_context);
            _context = null;

            _streams.ForEach(x => x.Dispose());
            _streams.Clear();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    static void CloseContext(AVFormatContext* context)
    {
        if (context is not null)
        {
            ffmpeg.av_read_pause(context);
            ffmpeg.avformat_flush(context);
        }

        ffmpeg.avformat_close_input(&context);
        ffmpeg.avformat_free_context(context);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
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
