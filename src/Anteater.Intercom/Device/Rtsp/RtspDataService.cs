using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using RtspClientSharp;
using RtspClientSharp.Decoding;
using RtspClientSharp.Decoding.DecodedFrames;
using RtspClientSharp.Decoding.FFmpeg;
using RtspClientSharp.RawFrames;
using RtspClientSharp.RawFrames.Audio;
using RtspClientSharp.RawFrames.Video;

namespace Anteater.Intercom.Device.Rtsp
{
    public class RtspDataService : BackgroundService
    {
        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

        private readonly Dictionary<FFmpegAudioCodecId, FFmpegAudioDecoder> _audioDecodersMap = new Dictionary<FFmpegAudioCodecId, FFmpegAudioDecoder>();
        private readonly Dictionary<FFmpegVideoCodecId, FFmpegVideoDecoder> _videoDecodersMap = new Dictionary<FFmpegVideoCodecId, FFmpegVideoDecoder>();

        private readonly Subject<IDecodedAudioFrame> _audioFrames = new Subject<IDecodedAudioFrame>();
        private readonly Subject<IDecodedVideoFrame> _videoFrames = new Subject<IDecodedVideoFrame>();

        private IObservable<IDecodedAudioFrame> _audioSource;
        private IObservable<IDecodedVideoFrame> _videoSource;

        private IDisposable _audioSubscription;
        private IDisposable _videoSubscription;

        public IObservable<IDecodedAudioFrame> AsAudioObservable() => _audioFrames.AsObservable();

        public IObservable<IDecodedVideoFrame> AsVideoObservable() => _videoFrames.AsObservable();

        public void SetAudioState(bool stopped)
        {
            _audioSubscription?.Dispose();
            _audioSubscription = null;

            if (!stopped)
            {
                _audioSubscription = _audioSource?.Subscribe(_audioFrames.OnNext) ?? Disposable.Empty;
            }
        }

        public void SetVideoState(bool stopped)
        {
            _videoSubscription?.Dispose();
            _videoSubscription = null;

            if (!stopped)
            {
                _videoSubscription = _videoSource?.Subscribe(_videoFrames.OnNext) ?? Disposable.Empty;
            }
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            SetAudioState(false);
            SetVideoState(false);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var endpoint = new Uri($"rtsp://{ConnectionSettings.Default.Username}:{ConnectionSettings.Default.Password}@{ConnectionSettings.Default.Host}:554/av0_0");

                    var connectionParameters = new ConnectionParameters(endpoint)
                    {
                        RtpTransport = RtpTransportProtocol.TCP,
                        CancelTimeout = TimeSpan.FromSeconds(1)
                    };

                    using var client = new RtspClient(connectionParameters);

                    _audioSource = GetAudioDecodingObservable(client);
                    _videoSource = GetVideoDecodingObservable(client);

                    if (_audioSubscription != null)
                    {
                        SetAudioState(false);
                    }

                    if (_videoSubscription != null)
                    {
                        SetVideoState(false);
                    }

                    await client.ConnectAsync(cancellationToken).ConfigureAwait(false);

                    await client.ReceiveAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch
                {
                    await Task.Delay(RetryDelay, cancellationToken);
                }
            }

            SetAudioState(true);
            SetVideoState(true);
        }

        IObservable<IDecodedAudioFrame> GetAudioDecodingObservable(RtspClient client) => Observable
            .FromEventPattern<RawFrame>(h => client.FrameReceived += h, h => client.FrameReceived -= h)
            .Select(x => x.EventArgs)
            .OfType<RawAudioFrame>()
            .Select(x => (Frame: x, Decoder: GetDecoderForFrame(x)))
            .Where(x => x.Decoder.TryDecode(x.Frame))
            .Select(x => x.Decoder.GetDecodedFrame(new AudioConversionParameters() { OutBitsPerSample = 16 }));

        IObservable<IDecodedVideoFrame> GetVideoDecodingObservable(RtspClient client) => Observable
            .FromEventPattern<RawFrame>(h => client.FrameReceived += h, h => client.FrameReceived -= h)
            .Select(x => x.EventArgs)
            .OfType<RawVideoFrame>()
            .Select(x => (Frame: x, Decoder: GetDecoderForFrame(x)))
            .Select(x => x.Decoder.TryDecode(x.Frame))
            .Where(x => x != null);

        FFmpegAudioDecoder GetDecoderForFrame(RawAudioFrame audioFrame)
        {
            var codecId = DetectCodecId(audioFrame);

            if (!_audioDecodersMap.TryGetValue(codecId, out var decoder))
            {
                int bitsPerCodedSample = 0;

                if (audioFrame is RawG726Frame g726Frame)
                {
                    bitsPerCodedSample = g726Frame.BitsPerCodedSample;
                }

                decoder = FFmpegAudioDecoder.CreateDecoder(codecId, bitsPerCodedSample);

                _audioDecodersMap.Add(codecId, decoder);
            }

            return decoder;
        }

        FFmpegVideoDecoder GetDecoderForFrame(RawVideoFrame videoFrame)
        {
            var codecId = DetectCodecId(videoFrame);

            if (!_videoDecodersMap.TryGetValue(codecId, out var decoder))
            {
                decoder = FFmpegVideoDecoder.CreateDecoder(codecId);

                _videoDecodersMap.Add(codecId, decoder);
            }

            return decoder;
        }

        static FFmpegAudioCodecId DetectCodecId(RawAudioFrame frame) => frame switch
        {
            RawAACFrame _ => FFmpegAudioCodecId.AAC,

            RawG711AFrame _ => FFmpegAudioCodecId.G711A,

            RawG711UFrame _ => FFmpegAudioCodecId.G711U,

            RawG726Frame _ => FFmpegAudioCodecId.G726,

            _ => throw new ArgumentOutOfRangeException(nameof(frame))
        };

        static FFmpegVideoCodecId DetectCodecId(RawVideoFrame frame) => frame switch
        {
            RawJpegFrame _ => FFmpegVideoCodecId.MJPEG,

            RawH264Frame _ => FFmpegVideoCodecId.H264,

            _ => throw new ArgumentOutOfRangeException(nameof(frame))
        };
    }
}
