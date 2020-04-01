using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Anteater.Intercom.Device.Audio.TcpTransport;
using Anteater.Intercom.Device.Events;

namespace Anteater.Intercom.Device.Audio
{
    public class BackChannelConnection
    {
        private readonly AlarmEventsService _alarmEvents;

        private bool _isDuplexMode;

        private TcpClient _client;
        private ExtFrameAudioPacketFactory _frameFactory;

        public BackChannelConnection(AlarmEventsService alarmEvents)
        {
            _alarmEvents = alarmEvents ?? throw new ArgumentNullException(nameof(alarmEvents));

            _alarmEvents.AsObservable()
                .Where(x => x.Type == AlarmEvent.EventType.SensorAlarm && x.Status)
                .Subscribe(e =>
                {
                    if (e.Numbers.FirstOrDefault() == 1)
                    {
                        Connect();
                        _isDuplexMode = false;
                    }
                });
        }

        public bool IsOpen { get; private set; }

        public int AudioSamples { get; private set; }

        public int AudioChannels { get; private set; }

        public int AudioBits { get; private set; }

        public void Connect()
        {
            try
            {
                _isDuplexMode = true;

                var settings = ConnectionSettings.Default;

                _client = new TcpClient();
                _client.Connect(settings.Host, settings.DataPort);

                var stream = _client.GetStream();

                stream.Write(new AceptHeader
                {
                    Username = settings.Username,
                    Password = settings.Password,
                    Flag = 17767,
                    SocketType = 2,
                    Misc = 0
                }.ToBytes());

                stream.Flush();

                var commHeader = CommHeader.Read(stream);

                switch (commHeader.ErrorCode)
                {
                    case 10:
                    case 8:
                        throw new InvalidOperationException("Failed to open audio backchannel.");
                }

                var tcpInfoHeader = TalkInfoHeader.Read(stream);

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

        public void Send(short[] data)
        {
            try
            {
                if (IsOpen)
                {
                    var stream = _client.GetStream();

                    stream.Write(_frameFactory.Create(data));
                    stream.Flush();
                }
            }
            catch
            {
                Disconnect();
                throw;
            }
        }

        public IObservable<(bool Status, string Message)> UnlockDoor()
        {
            var isDuplexMode = _isDuplexMode;

            if (_isDuplexMode)
            {
                Disconnect();
            }

            return Observable
                .FromAsync(UnlockDoorAsync)
                .Select(response =>
                {
                    if (!response.Status)
                    {
                        return Observable.Return(response);
                    }
                    else
                    {
                        return _alarmEvents.AsObservable()
                            .Where(x => x.Type == AlarmEvent.EventType.SensorOutAlarm && x.Status == false)
                            .Timeout(TimeSpan.FromSeconds(10))
                            .Select(x => (response.Status, response.Message))
                            .OnErrorResumeNext<(bool Status, string Message)>(Observable.Return((false, "Device is not healthy.")));
                    }
                })
                .Switch()
                .OnErrorResumeNext<(bool Status, string Message)>(Observable.Return((false, "Unable to unlock door.")))
                .Take(1)
                .Do(_ =>
                {
                    if (isDuplexMode)
                    {
                        Connect();
                    }
                });
        }

        static async Task<(bool Status, string Message)> UnlockDoorAsync()
        {
            var settings = ConnectionSettings.Default;

            var enpoint = new Uri($"http://{settings.Host}:{settings.WebPort}/cgi-bin/alarmout_cgi?action=set&user={settings.Username}&pwd={settings.Password}&Output=0&Status=1");

            using var client = new HttpClient();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{settings.Username}:{settings.Password}")));

            var response = await client.GetAsync(enpoint).ConfigureAwait(false);
            var content = response.Content != null ? await response.Content.ReadAsStringAsync().ConfigureAwait(false) : string.Empty;

            return (response.IsSuccessStatusCode, content);
        }

        public void Disconnect()
        {
            IsOpen = false;
            _client?.Dispose();
        }
    }
}
