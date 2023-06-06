using System.Text.Json;
using Anteater.Intercom.Services.Events;
using Anteater.Intercom.Services.Settings;
using CommunityToolkit.Mvvm.Messaging;
using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Hosting;

namespace Anteater.Intercom;

public class RemoteNotificationsService : BackgroundService, IRecipient<AlarmEvent>
{
    private readonly FirebaseMessaging _firebaseMessaging;
    private readonly ISettingsService _settings;

    private int _motionAlertsCount;
    private long _motionAlertTimestamp;
    private DateTime _motionAlertSent = DateTime.UtcNow;

    public RemoteNotificationsService(FirebaseMessaging firebaseMessaging, ISettingsService settings, IMessenger messenger)
    {
        _firebaseMessaging = firebaseMessaging;
        _settings = settings;

        messenger.RegisterAll(this);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tcs = new TaskCompletionSource();

        stoppingToken.Register(() => tcs.TrySetResult());

        await tcs.Task;
    }

    void IRecipient<AlarmEvent>.Receive(AlarmEvent message)
    {
        if (!message.Status || string.IsNullOrWhiteSpace(_settings.Current.DeviceId))
        {
            return;
        }

        if (message.Type == AlarmEvent.EventType.SensorAlarm)
        {
            _motionAlertTimestamp = 0;
            _motionAlertsCount = 0;

            _firebaseMessaging.SendAsync(new()
            {
                Topic = $"{_settings.Current.DeviceId}.call",
                Notification = new() { Title = "Incoming call", Body = "You received an inner door call" },
                Data = new Dictionary<string, string>
                {
                    { "event", JsonSerializer.Serialize(message) },
                },
                Apns = new() { Aps = new() { Sound = "doorbell.wav" } },
            });
        }

        if (message.Type == AlarmEvent.EventType.MotionDetection)
        {
            if (TimeSpan.FromTicks(DateTime.UtcNow.Ticks - _motionAlertTimestamp).TotalSeconds > 5)
            {
                _motionAlertTimestamp = 0;
                _motionAlertsCount = 0;
            }

            _motionAlertTimestamp = DateTime.UtcNow.Ticks;
            _motionAlertsCount++;

            if (_motionAlertsCount == 5)
            {
                if ((DateTime.UtcNow - _motionAlertSent).TotalSeconds > 30)
                {
                    _firebaseMessaging.SendAsync(new()
                    {
                        Topic = $"{_settings.Current.DeviceId}.motion",
                        Notification = new() { Title = "Motion detected" },
                        Apns = new() { Aps = new() { Sound = "default" } },
                    });

                    _motionAlertSent = DateTime.UtcNow;
                }

                _motionAlertTimestamp = 0;
                _motionAlertsCount = 0;
            }
        }
    }
}
