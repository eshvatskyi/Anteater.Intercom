using System.Buffers;
using System.IO.Pipelines;
using System.Net.Http.Headers;
using System.Text;
using Anteater.Intercom.Services.Settings;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Anteater.Intercom.Services.Events;

public class AlarmEventsService : BackgroundService
{
    static readonly TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(10);
    static readonly TimeSpan ReConnectTimeout = TimeSpan.FromMinutes(30);
    static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(60);
    static readonly TimeSpan ContinueDelay = TimeSpan.FromSeconds(5);

    private readonly IMessenger _messenger;
    private readonly HttpClient _client;

    private ConnectionSettings _settings;
    private CancellationTokenSource _cts;

    public AlarmEventsService(IMessenger messenger, IOptionsMonitor<ConnectionSettings> connectionSettings)
    {
        _messenger = messenger;

        _client = new HttpClient()
        {
            Timeout = ConnectionTimeout
        };

        _settings = connectionSettings.CurrentValue;

        connectionSettings.OnChange(settings =>
        {
            _settings = settings;
            _cts?.Cancel();
        });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

                    _cts.CancelAfter(ReConnectTimeout);

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

                    using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, stoppingToken);

                    response.EnsureSuccessStatusCode();

                    using var stream = await response.Content.ReadAsStreamAsync(stoppingToken);

                    await ProcessMessagesAsync(PipeReader.Create(stream), _cts.Token);

                    await Task.Delay(ContinueDelay, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch
                {
                    await Task.Delay(RetryDelay, stoppingToken);
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
                _messenger.Send(alarmEvent);
            }
        }

        return reader.Position;
    }
}
