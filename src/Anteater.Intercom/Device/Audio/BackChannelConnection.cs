using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Anteater.Intercom.Device.Audio.TcpTransport;
using Anteater.Intercom.Device.Events;
using Anteater.Pipe;
using Microsoft.Extensions.Options;

namespace Anteater.Intercom.Device.Audio
{
    public class BackChannelConnection
    {
        private readonly IOptionsMonitor<ConnectionSettings> _connectionSettings;
        private readonly IEventPublisher _pipe;

        private bool _isDuplexMode;

        private TcpClient _client;
        private NetworkStream _stream;
        private ExtFrameAudioPacketFactory _frameFactory;

        public BackChannelConnection(IOptionsMonitor<ConnectionSettings> connectionSettings, IEventPublisher pipe)
        {
            _connectionSettings = connectionSettings;
            _pipe = pipe;

            _pipe.Subscribe<AlarmEvent>(x => x
                .Where(x => x.Status && x.Type == AlarmEvent.EventType.SensorAlarm)
                .Where(x => x.Numbers.FirstOrDefault() == 1)
                .DoAsync(async x =>
                {
                    await ConnectAsync();
                    _isDuplexMode = false;
                }));
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

                await _client.ConnectAsync(_connectionSettings.CurrentValue.Host, _connectionSettings.CurrentValue.DataPort);

                _stream = _client.GetStream();

                await _stream.WriteAsync(new AceptHeader
                {
                    Username = _connectionSettings.CurrentValue.Username,
                    Password = _connectionSettings.CurrentValue.Password,
                    Flag = 17767,
                    SocketType = 2,
                    Misc = 0
                }.ToBytes());
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

                _frameFactory = new ExtFrameAudioPacketFactory(tcpInfoHeader.AudioEncodeType, tcpInfoHeader.AudioSamples, tcpInfoHeader.AudioChannels);

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
                    await _stream.WriteAsync(_frameFactory.Create(data));
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

            _pipe.Subscribe<AlarmEvent>(x => x
                .Where(x => !x.Status && x.Type == AlarmEvent.EventType.SensorOutAlarm)
                .Do(x => tcs.TrySetResult()));

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
            var enpoint = new Uri($"http://{_connectionSettings.CurrentValue.Host}:{_connectionSettings.CurrentValue.WebPort}/cgi-bin/alarmout_cgi?action=set&user={_connectionSettings.CurrentValue.Username}&pwd={_connectionSettings.CurrentValue.Password}&Output=0&Status=1");

            using var client = new HttpClient();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_connectionSettings.CurrentValue.Username}:{_connectionSettings.CurrentValue.Password}")));

            var response = await client.GetAsync(enpoint);
            var content = response.Content != null ? await response.Content.ReadAsStringAsync() : string.Empty;

            return (response.IsSuccessStatusCode, content);
        }

        public void Disconnect()
        {
            IsOpen = false;
            _client?.Dispose();
        }
    }
}
