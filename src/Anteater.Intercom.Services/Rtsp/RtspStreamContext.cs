using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen.Abstractions;
using static Anteater.Intercom.Services.Rtsp.RtspStream;

namespace Anteater.Intercom.Services.Rtsp;

public unsafe class RtspStreamContext : IAsyncDisposable
{
    public delegate void FormatAvailableEventHandler(RtspStreamContext ctx, RtspStreamFormat format);
    public delegate void StoppedEventHandler(RtspStreamContext ctx);

    public event FormatAvailableEventHandler FormatAvailable;
    public event FrameDecodedEventHandler VideoFrameDecoded;
    public event FrameDecodedEventHandler AudioFrameDecoded;
    public event StoppedEventHandler Stopped;

    private readonly AVFormatContext* _context = null;
    private readonly List<RtspStream> _streams = new();

    private readonly CancellationTokenSource _stoppingCancellation;
    private readonly Task _executionTask;

    public static RtspStreamContext Create(string url)
    {
        if (!CanInitializeContext(url))
        {
            throw new Exception($"Host: {url} is not available.");
        }

        return new RtspStreamContext(url);
    }

    private RtspStreamContext(string url)
    {
        var context = ffmpeg.avformat_alloc_context();

        AVDictionary* stream_opts;

        ffmpeg.av_dict_set(&stream_opts, "rtsp_transport", HasUdpSupport(url) ? "udp" : "tcp", 0);
        ffmpeg.av_dict_set(&stream_opts, "timeout", $"{1 * 1000 * 1000}", 0);
        ffmpeg.av_dict_set(&stream_opts, "rtpflags", "send_bye", 0);

        if (ffmpeg.avformat_open_input(&context, url, null, &stream_opts) < 0)
        {
            CloseContext(context);

            throw new Exception($"Host: {url} is not available.");
        }

        if (ffmpeg.avformat_find_stream_info(context, null) < 0)
        {
            CloseContext(context);

            throw new Exception($"Host: {url} is not available.");
        }

        _context = context;

        _stoppingCancellation = new CancellationTokenSource();

        _executionTask = Task.Run(() =>
        {
            InitializeStreams();
            ReadFrames();

            CloseContext(_context);

            _streams.ForEach(x => x.Dispose());
            _streams.Clear();
        }, _stoppingCancellation.Token).ContinueWith(_ =>
        {
            Stopped?.Invoke(this);
        });
    }

    static void SetDebugMode()
    {
        ffmpeg.av_log_set_level(ffmpeg.AV_LOG_DEBUG);
        ffmpeg.av_log_set_callback((av_log_set_callback_callback)delegate (void* p0, int level, string format, byte* vl)
        {
            var lineSize = 1024;
            var lineBuffer = stackalloc byte[lineSize];
            var printPrefix = 1;

            ffmpeg.av_log_format_line(p0, level, format, vl, lineBuffer, lineSize, &printPrefix);

            Debug.Write(Marshal.PtrToStringAnsi((IntPtr)lineBuffer));
        });
    }

    static bool HasUdpSupport(string url)
    {
        var uri = new Uri(url);

        if (uri.HostNameType == UriHostNameType.IPv4)
        {
            return IPAddress.Parse(uri.Host).AddressFamily == AddressFamily.InterNetwork;
        }

        return false;
    }

    static bool CanInitializeContext(string url)
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

        ffmpeg.av_dict_set(&stream_opts, "rtsp_transport", HasUdpSupport(url) ? "udp" : "tcp", 0);
        ffmpeg.av_dict_set(&stream_opts, "rtpflags", "send_bye", 0);

        var result = ffmpeg.avformat_open_input(&context, url, null, &stream_opts) >= 0;

        CloseContext(context);

        return result;
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

    void InitializeStreams()
    {
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

    void ReadFrames()
    {
        var packet = ffmpeg.av_packet_alloc();
        var frame = ffmpeg.av_frame_alloc();

        var formatAvailable = false;

        while (_context is not null && ffmpeg.av_read_frame(_context, packet) >= 0)
        {
            if (_stoppingCancellation.IsCancellationRequested)
            {
                break;
            }

            DecodeFrame(packet, frame);

            if (!formatAvailable)
            {
                formatAvailable = TryReadFormat();
            }

            ffmpeg.av_packet_unref(packet);
            ffmpeg.av_frame_unref(frame);
        }

        ffmpeg.av_packet_free(&packet);
        ffmpeg.av_frame_free(&frame);
    }

    bool TryReadFormat()
    {
        var vStream = _streams.OfType<RtspStreamVideo>().FirstOrDefault();
        if (vStream is null || vStream.Format is null)
        {
            return false;
        }

        var aStream = _streams.OfType<RtspStreamAudio>().FirstOrDefault();
        if (aStream is null || aStream.Format is null)
        {
            return false;
        }

        var format = new RtspStreamFormat(Video: vStream.Format, Audio: aStream.Format);

        FormatAvailable?.Invoke(this, format);

        return true;
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

    public ValueTask DisposeAsync()
    {
        _stoppingCancellation.Cancel();

        return new ValueTask(_executionTask);
    }
}
