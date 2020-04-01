using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Anteater.Intercom.Device.Events
{
    public class AlarmEventsService : BackgroundService
    {
        static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

        private readonly Subject<AlarmEvent> _events = new Subject<AlarmEvent>();

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var settings = ConnectionSettings.Default;

                        var endpoint = new Uri($"http://{settings.Host}/cgi-bin/alarmchangestate_cgi?user={settings.Username}&pwd={settings.Password}&parameter=MotionDetection;SensorAlarm;SensorOutAlarm");

                        using var client = new HttpClient();

                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{settings.Username}:{settings.Password}")));

                        client.Timeout = Timeout.InfiniteTimeSpan;

                        using var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, endpoint), HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

                        using var body = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

                        using var reader = new StreamReader(body);

                        while (!cancellationToken.IsCancellationRequested)
                        {
                            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

                            var tcs = new TaskCompletionSource<string>();

                            cts.Token.Register(() => tcs.TrySetException(new TimeoutException("Failer to read event source.")));

                            _ = reader.ReadLineAsync().ContinueWith(x => tcs.TrySetResult(x.Result));

                            var line = await tcs.Task.ConfigureAwait(false);

                            if (AlarmEvent.TryParse(line, out var alarmEvent))
                            {
                                try
                                {
                                    _events.OnNext(alarmEvent);
                                }
                                catch { }
                            }

                            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                        }
                    }
                    catch
                    {
                        await Task.Delay(RetryDelay, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        public IObservable<AlarmEvent> AsObservable() => _events.AsObservable();
    }
}
