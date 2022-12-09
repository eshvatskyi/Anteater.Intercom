using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Anteater.Intercom.Services.Events;
using Anteater.Intercom.Services.ReversChannel.Headers;
using CommunityToolkit.Mvvm.Messaging;
using FFmpeg.AutoGen.Abstractions;
using Microsoft.Extensions.Options;

namespace Anteater.Intercom.Services.ReversChannel;

public class ReversChannelService : IReversAudioService, IDoorLockService, IRecipient<AlarmEvent>
{
    private readonly IMessenger _messenger;

    private ConnectionSettings _settings;
    private bool _isDuplexMode;

    private TcpClient _client;
    private NetworkStream _stream;
    private BinaryWriter _writer;
    private AudioPacketFactory _audioPacketFactory;
    private QueuedBuffer _buffer;
    private AudioEncoder _encoder;

    public ReversChannelService(IMessenger messenger, IOptionsMonitor<ConnectionSettings> connectionSettings)
    {
        _messenger = messenger;

        _messenger.Register(this);

        _settings = connectionSettings.CurrentValue;

        connectionSettings.OnChange(settings =>
        {
            if (_settings == settings)
            {
                return;
            }

            _settings = settings;
        });
    }

    public bool IsOpen { get; private set; }

    public async Task ConnectAsync(AVSampleFormat format, int sampleRate, int channels)
    {
        try
        {
            _isDuplexMode = true;

            var infoHeader = await OpenAsync();

            _audioPacketFactory = new AudioPacketFactory(infoHeader, _writer);

            _buffer = new QueuedBuffer(_audioPacketFactory.BufferSize * 3);

            _encoder = GetEncoder(infoHeader, format, sampleRate, channels);
        }
        catch
        {
            Disconnect();
            throw;
        }
    }

    async Task<TalkInfoHeader> OpenAsync()
    {
        try
        {
            _client = new TcpClient();
            _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

            await _client.ConnectAsync(_settings.Host, _settings.DataPort);

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

            IsOpen = true;

            return await TalkInfoHeader.ReadAsync(_stream);
        }
        catch
        {
            Disconnect();
            throw;
        }
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

    public async Task SendAsync(byte[] data)
    {
        try
        {
            if (IsOpen)
            {
                await Task.Run(async delegate
                {
                    var encodedData = _encoder.Encode(data);

                    SendEncodedData(encodedData);

                    await _stream.FlushAsync();
                });
            }
        }
        catch
        {
            Disconnect();
            throw;
        }
    }

    void SendEncodedData(byte[] data)
    {
        _buffer.Write(data, 0, data.Length);

        Span<byte> frameData = stackalloc byte[_audioPacketFactory.BufferSize];

        while (_buffer.Length >= _audioPacketFactory.BufferSize)
        {
            _buffer.Read(frameData, 0, _audioPacketFactory.BufferSize);

            _audioPacketFactory.Write(frameData);
        }
    }

    public async Task<(bool Status, string Message)> UnlockDoorAsync()
    {
        var isDuplexMode = _isDuplexMode;

        if (_isDuplexMode)
        {
            Disconnect();
        }

        var (status, message) = await SendUnlockDoorAsync();

        if (!status)
        {
            return (false, message);
        }

        var tcs = new TaskCompletionSource();

        _messenger.Register<AlarmEvent>(tcs, (_, message) =>
        {
            if (message.Status == false && message.Type == AlarmEvent.EventType.SensorOutAlarm)
            {
                tcs.TrySetResult();
                _messenger.Unregister<AlarmEvent>(tcs);
            }
        });

        if (tcs.Task != await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(10))))
        {
            return (false, "Device is not healthy.");
        }

        await tcs.Task;

        if (isDuplexMode)
        {
            _ = OpenAsync();
        }

        return (true, "");
    }

    async Task<(bool Status, string Message)> SendUnlockDoorAsync()
    {
        var uriBuilder = new UriBuilder
        {
            Scheme = "http",
            Host = _settings.Host,
            Port = _settings.WebPort,
            Path = "cgi-bin/alarmout_cgi",
            Query = $"action=set&user={_settings.Username}&pwd={_settings.Password}&Output=0&Status=1",
        };

        using var client = new HttpClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_settings.Username}:{_settings.Password}")));

        var response = await client.GetAsync(uriBuilder.Uri);
        var content = response.Content != null ? await response.Content.ReadAsStringAsync() : string.Empty;

        return (response.IsSuccessStatusCode, content);
    }

    public void Disconnect()
    {
        IsOpen = false;
        _client?.Dispose();
        _audioPacketFactory?.Dispose();
    }

    async void IRecipient<AlarmEvent>.Receive(AlarmEvent message)
    {
        if (message.Status && message.Type == AlarmEvent.EventType.SensorAlarm && message.Numbers is [1, ..])
        {
            await OpenAsync();
            _isDuplexMode |= false;
        }
    }
}
