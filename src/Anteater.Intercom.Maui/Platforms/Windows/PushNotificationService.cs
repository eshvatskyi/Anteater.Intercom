using System.Text.Json;
using Anteater.Intercom.Services.Events;
using Anteater.Intercom.Services.Settings;
using CommunityToolkit.Mvvm.Messaging;
using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Anteater.Intercom;

public class PushNotificationService : BackgroundService, IRecipient<AlarmEvent>
{
    private readonly FirebaseMessaging _firebaseMessaging;

    private ConnectionSettings _settings;

    private int _motionAlertsCount;
    private long _motionAlertTimestamp;

    public PushNotificationService(FirebaseMessaging firebaseMessaging, IOptionsMonitor<ConnectionSettings> settings, IMessenger messenger)
    {
        _firebaseMessaging = firebaseMessaging;

        _settings = settings.CurrentValue;

        settings.OnChange(settings => _settings = settings);

        messenger.RegisterAll(this);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tcs = new TaskCompletionSource();

        stoppingToken.Register(tcs.SetCanceled);

        await tcs.Task;
    }

    void IRecipient<AlarmEvent>.Receive(AlarmEvent message)
    {
        if (!message.Status || string.IsNullOrWhiteSpace(_settings.DeviceId))
        {
            return;
        }

        if (message.Type == AlarmEvent.EventType.SensorAlarm)
        {
            _motionAlertTimestamp = 0;
            _motionAlertsCount = 0;

            _firebaseMessaging.SendAsync(new()
            {
                Topic = $"{_settings.DeviceId}.call",
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
            if (TimeSpan.FromTicks(DateTime.UtcNow.Ticks - _motionAlertTimestamp).TotalSeconds > 3)
            {
                _motionAlertTimestamp = 0;
                _motionAlertsCount = 0;
            }

            _motionAlertTimestamp = DateTime.UtcNow.Ticks;
            _motionAlertsCount++;

            if (_motionAlertsCount == 5)
            {
                _firebaseMessaging.SendAsync(new()
                {
                    Topic = $"{_settings.DeviceId}.motion",
                    Notification = new() { Title = "Motion detected" },
                    Apns = new() { Aps = new() { Sound = "default" } },
                });

                _motionAlertTimestamp = 0;
                _motionAlertsCount = 0;
            }            
        }
    }
}
