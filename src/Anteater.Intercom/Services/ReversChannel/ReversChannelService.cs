using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Anteater.Intercom.Services.ReversChannel.Headers;
using Anteater.Intercom.Services.Events;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Options;

namespace Anteater.Intercom.Services.ReversChannel;

public class ReversChannelService : IReversAudioService, IDoorLockService, IRecipient<AlarmEvent>
{
    private readonly IMessenger _messenger;

    private ConnectionSettings _settings;
    private bool _isDuplexMode;

    private TcpClient _client;
    private NetworkStream _stream;
    private AudioPacketFactory _audioPacketFactory;

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

    public int AudioSamples { get; private set; }

    public int AudioChannels { get; private set; }

    public int AudioBits { get; private set; }

    public async Task ConnectAsync()
    {
        try
        {
            _isDuplexMode = true;

            _client = new TcpClient();
            _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

            await _client.ConnectAsync(_settings.Host, _settings.DataPort);

            _stream = _client.GetStream();

            await _stream.WriteAsync(new AcceptHeader
            {
                Username = _settings.Username,
                Password = _settings.Password,
                Flag = 17767,
                SocketType = 2,
                Misc = 0
            });

            await _stream.FlushAsync();

            var commHeader = await CommHeader.ReadAsync(_stream);

            switch (commHeader.ErrorCode)
            {
                case 10:
                case 8:
                    throw new InvalidOperationException("Failed to open audio backchannel.");
            }

            var tcpInfoHeader = await TalkInfoHeader.ReadAsync(_stream);

            AudioSamples = tcpInfoHeader.AudioSamples;
            AudioChannels = tcpInfoHeader.AudioChannels;
            AudioBits = tcpInfoHeader.AudioBits;

            _audioPacketFactory = new AudioPacketFactory(tcpInfoHeader.AudioEncodeType, tcpInfoHeader.AudioSamples, tcpInfoHeader.AudioChannels);

            IsOpen = true;
        }
        catch
        {
            Disconnect();
            throw;
        }
    }

    public async Task SendAsync(short[] data)
    {
        try
        {
            if (IsOpen)
            {
                await _stream.WriteAsync(_audioPacketFactory.Create(data));
                await _stream.FlushAsync();
            }
        }
        catch
        {
            Disconnect();
            throw;
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
            _ = ConnectAsync();
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
    }

    async void IRecipient<AlarmEvent>.Receive(AlarmEvent message)
    {
        if (message.Status && message.Type == AlarmEvent.EventType.SensorAlarm && message.Numbers is [1, ..])
        {
            await ConnectAsync();
            _isDuplexMode |= false;
        }
    }
}
