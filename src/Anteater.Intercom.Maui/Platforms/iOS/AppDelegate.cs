using System.Text.Json;
using Anteater.Intercom.Services.Events;
using Anteater.Intercom.Services.Settings;
using CommunityToolkit.Mvvm.Messaging;
using Firebase.CloudMessaging;
using Foundation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Maui.LifecycleEvents;
using UIKit;
using UserNotifications;

namespace Anteater.Intercom;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate, IUNUserNotificationCenterDelegate, IMessagingDelegate
{
    private IMessenger _messenger;
    private ConnectionSettings _settings;
    private ILogger<AppDelegate> _logger;

    protected override MauiApp CreateMauiApp()
    {
        var app = MauiProgram.CreateMauiApp(ConfigurePlatformServices);

        _messenger = app.Services.GetRequiredService<IMessenger>();

        var settingsMonitor = app.Services.GetRequiredService<IOptionsMonitor<ConnectionSettings>>();

        _settings = settingsMonitor.CurrentValue;

        settingsMonitor.OnChange(OnSettingsChanged);

        _logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger<AppDelegate>();

        return app;
    }

    private MauiAppBuilder ConfigurePlatformServices(MauiAppBuilder builder)
    {
        builder.ConfigureLifecycleEvents(events =>
        {
            events.AddiOS(x => x.FinishedLaunching((app, opts) =>
            {
                var authOptions = UNAuthorizationOptions.Alert | UNAuthorizationOptions.Badge | UNAuthorizationOptions.Sound;

                UNUserNotificationCenter.Current.RequestAuthorization(authOptions, (granted, error) =>
                {
                    if (granted && error is null)
                    {
                        InvokeOnMainThread(() =>
                        {
                            UIApplication.SharedApplication.RegisterForRemoteNotifications();

                            Firebase.Core.App.Configure();

                            Messaging.SharedInstance.Delegate = this;

                            UNUserNotificationCenter.Current.Delegate = this;
                        });
                    }
                });

                return true;
            }));
        });

        return builder;
    }

    [Export("application:didRegisterForRemoteNotificationsWithDeviceToken:")]
    public void RemoteNotificationsRegistrationCompleted(UIApplication application, NSData deviceToken)
    {
        _logger.LogDebug($"Remote notifications registration completed.");
    }

    [Export("application:didFailToRegisterForRemoteNotificationsWithError:")]
    public void RemoteNotificationsRegistrationFailed(UIApplication application, NSError error)
    {
        _logger.LogError($"Remote notifications registration failed: {error?.LocalizedDescription}.");
    }

    [Export("messaging:didReceiveRegistrationToken:")]
    public void RemoteNotificationsRegistrationTokenReceived(Messaging messaging, string fcmToken)
    {
        _logger.LogDebug($"Firebase token: {fcmToken}.");

        if (!string.IsNullOrWhiteSpace(_settings?.DeviceId))
        {
            Messaging.SharedInstance.Subscribe($"{_settings.DeviceId}.call");
            Messaging.SharedInstance.Subscribe($"{_settings.DeviceId}.motion");
        }
    }

    [Export("application:didReceiveRemoteNotification:fetchCompletionHandler:")]
    public void RemoteNotificationsNotificationResponseReceived(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
    {
        _logger.LogDebug($"Remote notification tapped: {NSJsonSerialization.Serialize(userInfo, 0, out _)}.");

        if (userInfo?.TryGetValue((NSString)"event", out var eventData) ?? false)
        {
            _messenger.Send(JsonSerializer.Deserialize<AlarmEvent>((NSString)eventData));
        }

        completionHandler(UIBackgroundFetchResult.NewData);
    }

    [Export("userNotificationCenter:willPresentNotification:withCompletionHandler:")]
    public void RemoteNotificationsWillPresentNotification(UNUserNotificationCenter center, UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler)
    {
        var userInfo = notification.Request.Content.UserInfo;

        _logger.LogDebug($"Remote notification received in foreground: {NSJsonSerialization.Serialize(userInfo, 0, out _)}.");

        if (userInfo?.TryGetValue((NSString)"event", out var eventData) ?? false)
        {
            _messenger.Send(JsonSerializer.Deserialize<AlarmEvent>((NSString)eventData));
        }

        completionHandler(UNNotificationPresentationOptions.None);
    }

    void OnSettingsChanged(ConnectionSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(_settings?.DeviceId))
        {
            Messaging.SharedInstance.Unsubscribe($"{_settings.DeviceId}.call");
            Messaging.SharedInstance.Unsubscribe($"{_settings.DeviceId}.motion");
        }

        if (!string.IsNullOrWhiteSpace(settings?.DeviceId))
        {
            Messaging.SharedInstance.Subscribe($"{settings.DeviceId}.call");
            Messaging.SharedInstance.Subscribe($"{settings.DeviceId}.motion");
        }

        _settings = settings;
    }
}
