using Anteater.Intercom.Services.Settings;
using CommunityToolkit.Mvvm.Messaging;
using Firebase.CloudMessaging;
using Foundation;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using UIKit;
using UserNotifications;

namespace Anteater.Intercom;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate, IUNUserNotificationCenterDelegate, IMessagingDelegate
{
    private IMessenger _messenger;
    private ISettingsService _settings;
    private ILogger<AppDelegate> _logger;

    protected override MauiApp CreateMauiApp()
    {
        var app = MauiProgram.CreateMauiApp(ConfigurePlatformServices);

        _messenger = app.Services.GetRequiredService<IMessenger>();

        _settings = app.Services.GetRequiredService<ISettingsService>();
        _settings.Changed += OnSettingsChanged;

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
    public void RemoteNotificationsRegistrationCompleted(UIApplication application, NSData apnsToken)
    {
        _logger.LogDebug($"Remote notifications registration completed.");

        Messaging.SharedInstance.SetApnsToken(apnsToken, ApnsTokenType.Unknown);
    }

    [Export("application:didFailToRegisterForRemoteNotificationsWithError:")]
    public void RemoteNotificationsRegistrationFailed(UIApplication application, NSError error)
    {
        _logger.LogError($"Remote notifications registration failed: {error?.LocalizedDescription}.");
    }

    [Export("messaging:didReceiveRegistrationToken:")]
    public void RemoteNotificationsRegistrationTokenReceived(Messaging messaging, string apnsToken)
    {
        _logger.LogDebug($"Remote notifications token received: {apnsToken}.");

        if (!string.IsNullOrWhiteSpace(_settings.Current.DeviceId))
        {
            messaging.Subscribe($"{_settings.Current.DeviceId}.call");
            messaging.Subscribe($"{_settings.Current.DeviceId}.motion");
        }
    }

    //[Export("application:didReceiveRemoteNotification:fetchCompletionHandler:")]
    //public void RemoteNotificationsNotificationResponseReceived(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
    //{
    //    _logger.LogDebug($"Remote notification tapped: {NSJsonSerialization.Serialize(userInfo, 0, out _)}.");

    //    if (userInfo?.TryGetValue((NSString)"event", out var eventData) ?? false)
    //    {
    //        _messenger.Send(JsonSerializer.Deserialize<AlarmEvent>((NSString)eventData));
    //    }

    //    completionHandler(UIBackgroundFetchResult.NewData);
    //}

    //[Export("userNotificationCenter:willPresentNotification:withCompletionHandler:")]
    //public void RemoteNotificationsWillPresentNotification(UNUserNotificationCenter center, UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler)
    //{
    //    var userInfo = notification.Request.Content.UserInfo;

    //    _logger.LogDebug($"Remote notification received in foreground: {NSJsonSerialization.Serialize(userInfo, 0, out _)}.");

    //    if (userInfo?.TryGetValue((NSString)"event", out var eventData) ?? false)
    //    {
    //        _messenger.Send(JsonSerializer.Deserialize<AlarmEvent>((NSString)eventData));
    //    }

    //    completionHandler(UNNotificationPresentationOptions.None);
    //}

    void OnSettingsChanged(ConnectionSettings previous, ConnectionSettings current)
    {
        if (!string.IsNullOrWhiteSpace(previous?.DeviceId))
        {
            Messaging.SharedInstance.Unsubscribe($"{previous.DeviceId}.call");
            Messaging.SharedInstance.Unsubscribe($"{previous.DeviceId}.motion");
        }

        if (!string.IsNullOrWhiteSpace(current?.DeviceId))
        {
            Messaging.SharedInstance.Subscribe($"{current.DeviceId}.call");
            Messaging.SharedInstance.Subscribe($"{current.DeviceId}.motion");
        }
    }
}
