using System.Buffers;
using System.IO.Pipelines;
using System.Net.Http.Headers;
using System.Text;
using Anteater.Intercom.Services.Settings;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Hosting;

namespace Anteater.Intercom.Services.Events;

public class AlarmEventsService : BackgroundService
{
    static readonly TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(10);
    static readonly TimeSpan ReConnectTimeout = TimeSpan.FromMinutes(30);
    static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(60);
    static readonly TimeSpan ContinueDelay = TimeSpan.FromSeconds(5);

    private readonly IMessenger _messenger;
    private readonly ISettingsService _settings;
    private readonly HttpClient _client;

    private CancellationTokenSource _cts;

    public AlarmEventsService(IMessenger messenger, ISettingsService settings)
    {
        _messenger = messenger;

        _settings = settings;
        _settings.Changed += (_, _) => _cts?.Cancel();

        _client = new HttpClient()
        {
            Timeout = ConnectionTimeout
        };
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
                        Host = _settings.Current.Host,
                        Port = _settings.Current.WebPort,
                        Path = "cgi-bin/alarmchangestate_cgi",
                        Query = $"parameter=MotionDetection;SensorAlarm;SensorOutAlarm",
                    };

                    using var request = new HttpRequestMessage(HttpMethod.Get, uriBuilder.Uri);

                    request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_settings.Current.Username}:{_settings.Current.Password}")));

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
