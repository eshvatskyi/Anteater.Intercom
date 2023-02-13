using System.Net.Http.Headers;
using System.Text;
using Anteater.Intercom.Services.Events;
using Anteater.Intercom.Services.Settings;
using CommunityToolkit.Mvvm.Messaging;
using FFmpeg.AutoGen.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Anteater.Intercom.Services.ReversChannel;

public class ReversChannelService : BackgroundService, IReversAudioService, IDoorLockService, IRecipient<AlarmEvent>
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private readonly IMessenger _messenger;
    private readonly ISettingsService _settings;
    private readonly ILogger _logger;

    private ReversChannelClient _client;

    private bool _keepConnectionForUnlock = false;

    public ReversChannelService(IMessenger messenger, ISettingsService settings, ILoggerFactory logger)
    {
        _messenger = messenger;
        _messenger.Register(this);

        _settings = settings;

        _logger = logger.CreateLogger<ReversChannelService>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tcs = new TaskCompletionSource();

        stoppingToken.Register(() => tcs.TrySetResult());

        await tcs.Task;
    }

    async Task OpenAsync(bool threadSafe)
    {
        try
        {
            if (!threadSafe)
            {
                await _semaphore.WaitAsync();
            }

            _client = new ReversChannelClient(_settings.Current);

            await _client.OpenAsync();
        }
        catch
        {
            Disconnect(true);

            throw;
        }
        finally
        {
            if (!threadSafe)
            {
                _semaphore.Release();
            }
        }
    }

    void Disconnect(bool threadSafe)
    {
        try
        {
            if (!threadSafe)
            {
                _semaphore.Wait();
            }

            _keepConnectionForUnlock = false;

            _client?.Disconnect();
            _client = null;
        }
        finally
        {
            if (!threadSafe)
            {
                _semaphore.Release();
            }
        }
    }

    async ValueTask IReversAudioService.ConnectAsync()
    {
        try
        {
            await _semaphore.WaitAsync();

            if (_client is null)
            {
                _keepConnectionForUnlock = false;

                await OpenAsync(true);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    async Task IReversAudioService.SendAsync(AVSampleFormat format, int sampleRate, int channels, byte[] data)
    {
        try
        {
            await _semaphore.WaitAsync();

            if (_client is null)
            {
                return;
            }

            await _client.SendAsync(format, sampleRate, channels, data);
        }
        catch
        {
            Disconnect(true);
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    void IReversAudioService.Disconnect()
    {
        Disconnect(false);
    }

    async Task<(bool Status, string Message)> IDoorLockService.UnlockDoorAsync()
    {
        try
        {
            await _semaphore.WaitAsync();

            if (_client is not null && !_keepConnectionForUnlock)
            {
                Disconnect(true);
            }
        }
        finally
        {
            _semaphore.Release();
        }

        var (status, message) = await SendUnlockDoorAsync(_settings.Current);

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

        try
        {
            await _semaphore.WaitAsync();

            if (!_keepConnectionForUnlock)
            {
                await OpenAsync(true);
            }
        }
        finally
        {
            _semaphore.Release();
        }

        return (true, "");
    }

    static async Task<(bool Status, string Message)> SendUnlockDoorAsync(ConnectionSettings settings)
    {
        var uriBuilder = new UriBuilder
        {
            Scheme = "http",
            Host = settings.Host,
            Port = settings.WebPort,
            Path = "cgi-bin/alarmout_cgi",
            Query = $"action=set&Output=0&Status=1",
        };

        using var client = new HttpClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{settings.Username}:{settings.Password}")));

        var response = await client.GetAsync(uriBuilder.Uri);
        var content = response.Content != null ? await response.Content.ReadAsStringAsync() : "";

        return (response.IsSuccessStatusCode, content);
    }

    void IRecipient<AlarmEvent>.Receive(AlarmEvent message)
    {
        if (message.Status && message.Type == AlarmEvent.EventType.SensorAlarm && message.Numbers is [1, ..])
        {
            var timeout = TimeSpan.FromSeconds(30) - TimeSpan.FromTicks(DateTime.UtcNow.Ticks - message.Timestamp);

            _logger.LogDebug($"AlarmEvent.Received: With timeout, {timeout.TotalSeconds} secs");

            if (timeout.TotalSeconds > 0)
            {
                _keepConnectionForUnlock = true;

                if (_client is null)
                {
                    _ = OpenAsync(false);
                }
            }
        }
    }
}
