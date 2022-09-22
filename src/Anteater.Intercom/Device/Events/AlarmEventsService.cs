using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Anteater.Pipe;
using Microsoft.Extensions.Options;

namespace Anteater.Intercom.Device.Events
{
    public class AlarmEventsService : BackgroundService
    {
        static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

        private readonly IOptionsMonitor<ConnectionSettings> _connectionSettings;
        private readonly IEventPublisher _pipe;

        public AlarmEventsService(IOptionsMonitor<ConnectionSettings> connectionSettings, IEventPublisher pipe)
        {
            _connectionSettings = connectionSettings;
            _pipe = pipe;
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                var endpoint = new Uri($"http://{_connectionSettings.CurrentValue.Host}/cgi-bin/alarmchangestate_cgi?user={_connectionSettings.CurrentValue.Username}&pwd={_connectionSettings.CurrentValue.Password}&parameter=MotionDetection;SensorAlarm;SensorOutAlarm");

                using var client = new HttpClient();

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_connectionSettings.CurrentValue.Username}:{_connectionSettings.CurrentValue.Password}")));

                client.Timeout = Timeout.InfiniteTimeSpan;

                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        using var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, endpoint), HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

                        await ProcessMessagesAsync(PipeReader.Create(stream), cancellationToken);

                        await Task.Delay(100, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch
                    {
                        await Task.Delay(RetryDelay, cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        async Task ProcessMessagesAsync(PipeReader reader, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var readResult = await reader.ReadAsync(cancellationToken);

                    if (readResult.IsCanceled)
                    {
                        break;
                    }

                    reader.AdvanceTo(ParseEvents(readResult.Buffer));

                    if (readResult.IsCompleted)
                    {
                        break;
                    }
                }
            }
            finally
            {
                await reader.CompleteAsync();
            }
        }

        SequencePosition ParseEvents(ReadOnlySequence<byte> buffer)
        {
            var reader = new SequenceReader<byte>(buffer);

            while (reader.TryReadTo(out ReadOnlySequence<byte> line, (byte)'\n'))
            {
                if (AlarmEvent.TryParse(Encoding.ASCII.GetString(line), out var alarmEvent))
                {
                    _pipe.Publish(alarmEvent);
                }
            }

            return reader.Position;
        }
    }
}
