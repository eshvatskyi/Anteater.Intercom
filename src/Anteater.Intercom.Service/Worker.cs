using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Hosting;
using RtspClientSharp;
using RtspClientSharp.Decoding;
using RtspClientSharp.Decoding.DecodedFrames;
using RtspClientSharp.Decoding.FFmpeg;
using RtspClientSharp.RawFrames;
using RtspClientSharp.RawFrames.Audio;
using RtspClientSharp.RawFrames.Video;

namespace Anteater.Intercom.Service
{
    public class Worker : BackgroundService
    {
        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

        private readonly Dictionary<FFmpegAudioCodecId, FFmpegAudioDecoder> _audioDecodersMap = new Dictionary<FFmpegAudioCodecId, FFmpegAudioDecoder>();
        private readonly Dictionary<FFmpegVideoCodecId, FFmpegVideoDecoder> _videoDecodersMap = new Dictionary<FFmpegVideoCodecId, FFmpegVideoDecoder>();

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await InitializeKafka().ConfigureAwait(false);

            var config = new ProducerConfig
            {
                BootstrapServers = "10.0.1.15:9092"
            };

            using var audioProducer = new ProducerBuilder<Null, byte[]>(config).Build();
            using var videoProducer = new ProducerBuilder<Null, IDecodedVideoFrame>(config).Build();

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using var client = new RtspClient(new ConnectionParameters(new Uri($"rtsp://admin:Ababbagalamaga5@10.0.1.2:554/av0_0"))
                    {
                        RtpTransport = RtpTransportProtocol.TCP,
                        CancelTimeout = TimeSpan.FromSeconds(1)
                    });

                    using var audioSubscribtion = GetAudioDecodingObservable(client).Subscribe(async x =>
                    {
                        await audioProducer.ProduceAsync("anteater.audio", new Message<Null, byte[]>() { Value = x.DecodedBytes.Array ?? Array.Empty<byte>() }).ConfigureAwait(false);
                    });

                    using var videoSubscribtion = GetVideoDecodingObservable(client).Subscribe(async x =>
                    {
                        await videoProducer.ProduceAsync("anteater.video", new Message<Null, IDecodedVideoFrame>() { Value = x }).ConfigureAwait(false);
                    });

                    await client.ConnectAsync(cancellationToken).ConfigureAwait(false);

                    await client.ReceiveAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch
                {
                    await Task.Delay(RetryDelay, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        async ValueTask InitializeKafka()
        {
            var config = new AdminClientConfig
            {
                BootstrapServers = "10.0.1.15:9092"
            };

            using var client = new AdminClientBuilder(config).Build();

            var metadata = client.GetMetadata(TimeSpan.FromSeconds(20));

            var topicsToCreate = new List<TopicSpecification>();

            if (!metadata.Topics.Any(x => x.Topic == "anteater.video"))
            {
                topicsToCreate.Add(new TopicSpecification
                {
                    Name = "anteater.video",
                    ReplicationFactor = 1,
                    NumPartitions = 1,
                    Configs = new Dictionary<string, string>
                    {
                        ["cleanup.policy"] = "delete",
                        ["compression.type"] = "uncompressed",
                        ["retention.ms"] = "500"
                    }
                });
            }

            if (!metadata.Topics.Any(x => x.Topic == "anteater.audio"))
            {
                topicsToCreate.Add(new TopicSpecification
                {
                    Name = "anteater.audio",
                    ReplicationFactor = 1,
                    NumPartitions = 1,
                    Configs = new Dictionary<string, string>
                    {
                        ["cleanup.policy"] = "delete",
                        ["compression.type"] = "uncompressed",
                        ["retention.ms"] = "500"
                    }
                });
            }

            if (!metadata.Topics.Any(x => x.Topic == "anteater.event"))
            {
                topicsToCreate.Add(new TopicSpecification
                {
                    Name = "anteater.event",
                    ReplicationFactor = 1,
                    NumPartitions = 1,
                    Configs = new Dictionary<string, string>
                    {
                        ["cleanup.policy"] = "delete",
                        ["compression.type"] = "uncompressed",
                        ["retention.ms"] = "500"
                    }
                });
            }

            if (topicsToCreate.Any())
            {
                await client.CreateTopicsAsync(topicsToCreate).ConfigureAwait(false);
            }
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
