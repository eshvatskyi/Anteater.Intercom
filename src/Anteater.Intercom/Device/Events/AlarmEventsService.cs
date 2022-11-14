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

namespace Anteater.Intercom.Device.Events;

public class AlarmEventsService : BackgroundService
{
    static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(60);

    private readonly IEventPublisher _pipe;
    private readonly HttpClient _client;

    private ConnectionSettings _settings;
    private CancellationTokenSource _cts;

    public AlarmEventsService(IOptionsMonitor<ConnectionSettings> connectionSettings, IEventPublisher pipe)
    {
        _pipe = pipe;

        _client = new HttpClient
        {
            Timeout = Timeout.InfiniteTimeSpan
        };

        _settings = connectionSettings.CurrentValue;

        connectionSettings.OnChange(settings =>
        {
            if (_settings == settings)
            {
                return;
            }

            _settings = settings;
            _cts?.Cancel();
        });
    }

    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                    var uriBuilder = new UriBuilder
                    {
                        Scheme = "http",
                        Host = _settings.Host,
                        Port = _settings.WebPort,
                        Path = "cgi-bin/alarmchangestate_cgi",
                        Query = $"user={_settings.Username}&pwd={_settings.Password}&parameter=MotionDetection;SensorAlarm;SensorOutAlarm",
                    };

                    using var request = new HttpRequestMessage(HttpMethod.Get, uriBuilder.Uri);

                    request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_settings.Username}:{_settings.Password}")));

                    using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                    response.EnsureSuccessStatusCode();

                    using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

                    await ProcessMessagesAsync(PipeReader.Create(stream), _cts.Token);

                    await Task.Delay(RetryDelay, cancellationToken);
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
        catch (OperationCanceledException) { }
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
