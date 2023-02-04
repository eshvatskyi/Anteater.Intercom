using System.Text.Json;
using Anteater.Intercom.Services.Events;
using Anteater.Intercom.Services.Settings;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Plugin.Firebase.CloudMessaging;
using Plugin.Firebase.CloudMessaging.EventArgs;

namespace Anteater.Intercom;

public class PushNotificationService : BackgroundService
{
    private readonly IFirebaseCloudMessaging _firebaseMessaging;
    private readonly IMessenger _messenger;
    private ConnectionSettings _settings;
    private readonly ILogger _logger;

    public PushNotificationService(IFirebaseCloudMessaging firebaseMessaging, IMessenger messenger, IOptionsMonitor<ConnectionSettings> settings, ILoggerFactory logger)
    {
        _firebaseMessaging = firebaseMessaging;
        _messenger = messenger;

        _firebaseMessaging.NotificationTapped += OnNotificationTapped;

        _settings = settings.CurrentValue;

        settings.OnChange(OnSettingsChanged);

        _logger = logger.CreateLogger<PushNotificationService>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tcs = new TaskCompletionSource();

        stoppingToken.Register(tcs.SetCanceled);

        await tcs.Task;
    }

    void OnNotificationTapped(object sender, FCMNotificationTappedEventArgs e)
    {
        _logger.LogDebug($"OnNotificationTapped: {JsonSerializer.Serialize(e)}");

        if (e.Notification.Data?.TryGetValue("event", out var eventData) ?? false)
        {
            _messenger.Send(JsonSerializer.Deserialize<AlarmEvent>(eventData));
        }
    }

    void OnSettingsChanged(ConnectionSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(_settings?.DeviceId))
        {
            _ = _firebaseMessaging.UnsubscribeFromTopicAsync($"{_settings.DeviceId}.call");
            _ = _firebaseMessaging.UnsubscribeFromTopicAsync($"{_settings.DeviceId}.motion");
        }

        if (!string.IsNullOrWhiteSpace(settings?.DeviceId))
        {
            _ = _firebaseMessaging.SubscribeToTopicAsync($"{settings.DeviceId}.call");
            _ = _firebaseMessaging.SubscribeToTopicAsync($"{settings.DeviceId}.motion");
        }

        _settings = settings;
    }
}
