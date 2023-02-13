using System.Net;
using System.Net.Sockets;
using Anteater.Intercom.Services.ReversChannel.Headers;
using Anteater.Intercom.Services.Settings;
using FFmpeg.AutoGen.Abstractions;

namespace Anteater.Intercom.Services.ReversChannel;

public class ReversChannelClient
{
    private readonly ConnectionSettings _settings;

    private TcpClient _client;
    private NetworkStream _stream;
    private BinaryWriter _writer;
    private TalkInfoHeader _infoHeader;
    private AudioPacketFactory _audioPacketFactory;
    private QueuedBuffer _buffer;
    private AudioEncoder _encoder;

    public ReversChannelClient(ConnectionSettings settings)
    {
        _settings = settings;
    }

    public async Task OpenAsync()
    {
        try
        {
            _client = new TcpClient();
            _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

            if (!IPAddress.TryParse(_settings.Host, out var address))
            {
                address = Dns.GetHostEntry(_settings.Host).AddressList.FirstOrDefault();
            }

            await _client.ConnectAsync(address, _settings.DataPort);

            _stream = _client.GetStream();
            _writer = new BinaryWriter(_stream);

            new AcceptHeader
            {
                Username = _settings.Username,
                Password = _settings.Password,
                Flag = 17767,
                SocketType = 2,
                Misc = 0
            }.Write(_writer);

            await _stream.FlushAsync();

            var commHeader = await CommHeader.ReadAsync(_stream);

            switch (commHeader.ErrorCode)
            {
                case 10:
                case 8:
                    throw new InvalidOperationException("Failed to open audio backchannel.");
            }

            _infoHeader = await TalkInfoHeader.ReadAsync(_stream);

            _audioPacketFactory = new AudioPacketFactory(_infoHeader, _writer);

            _buffer = new QueuedBuffer(_audioPacketFactory.BufferSize * 3);
        }
        catch
        {
            Disconnect();
            throw;
        }
    }

    public async Task SendAsync(AVSampleFormat format, int sampleRate, int channels, byte[] data)
    {
        if (_client is null)
        {
            return;
        }

        try
        {
            if (_encoder is null)
            {
                _encoder = GetEncoder(_infoHeader, format, sampleRate, channels);
            }

            await Task.Run(() =>
            {
                var encodedData = _encoder.Encode(data);

                _buffer.Write(encodedData, 0, encodedData.Length);

                Span<byte> frameData = stackalloc byte[_audioPacketFactory.BufferSize];

                while (_buffer.Length >= _audioPacketFactory.BufferSize)
                {
                    _buffer.Read(frameData, 0, _audioPacketFactory.BufferSize);

                    _audioPacketFactory.Write(frameData);
                }
            });

            await _stream.FlushAsync();
        }
        catch
        {
            Disconnect();
            throw;
        }
    }

    public void Disconnect()
    {
        _client?.Dispose();
        _writer?.Dispose();
        _audioPacketFactory?.Dispose();

        _client = null;
        _writer = null;
        _audioPacketFactory = null;
        _buffer = null;
        _encoder = null;
    }

    static AudioEncoder GetEncoder(TalkInfoHeader info, AVSampleFormat format, int sampleRate, int channels)
    {
        var codecId = info.AudioEncodeType switch
        {
            7 => AVCodecID.AV_CODEC_ID_PCM_MULAW,
            3 => AVCodecID.AV_CODEC_ID_PCM_ALAW,
            1 => AVCodecID.AV_CODEC_ID_ADPCM_G726,
            _ => throw new InvalidOperationException("Unknown encoding type."),
        };

        return new AudioEncoder(codecId, info.AudioSamples, info.AudioChannels, format, sampleRate, channels);
    }
}
