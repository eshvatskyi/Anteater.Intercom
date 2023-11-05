using Anteater.Intercom.Core.Settings;
using CommunityToolkit.Mvvm.Messaging;
using Firebase.CloudMessaging;
using Foundation;
using Microsoft.Extensions.Logging;
using UIKit;
using UserNotifications;

namespace Anteater.Intercom;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate, IUNUserNotificationCenterDelegate, IMessagingDelegate
{
    private Messaging _messaging;
    private ISettingsService _settings;
    private IMessenger _messenger;
    private ILogger<AppDelegate> _logger;

    protected override MauiApp CreateMauiApp()
    {
        var app = MauiProgram.CreateMauiApp(ConfigurePlatformServices);

        _messaging = app.Services.GetRequiredService<Messaging>();
        _settings = app.Services.GetRequiredService<ISettingsService>();
        _messenger = app.Services.GetRequiredService<IMessenger>();

        _logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger<AppDelegate>();

        return app;
    }

    private MauiAppBuilder ConfigurePlatformServices(MauiAppBuilder builder)
    {
        Firebase.Core.App.Configure();

        builder.Services.AddSingleton(Messaging.SharedInstance);

        return builder;
    }

    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        Messaging.SharedInstance.Delegate = this;
        UNUserNotificationCenter.Current.Delegate = this;

        var authOptions = UNAuthorizationOptions.Alert | UNAuthorizationOptions.Badge | UNAuthorizationOptions.Sound;

        UNUserNotificationCenter.Current.RequestAuthorization(authOptions, (granted, error) =>
        {
            if (granted && error is null)
            {
                InvokeOnMainThread(() =>
                {
                    UIApplication.SharedApplication.RegisterForRemoteNotifications();
                });
            }
        });

        _settings.Changed += OnSettingsChanged;

        return base.FinishedLaunching(application, launchOptions);
    }

    public override void OnActivated(UIApplication application)
    {
        base.OnActivated(application);

        //UNUserNotificationCenter.Current.RemoveAllDeliveredNotifications();
    }

    [Export("application:didRegisterForRemoteNotificationsWithDeviceToken:")]
    public void DidRegisterForRemoteNotificationsWithDeviceToken(UIApplication application, NSData apnsToken)
    {
        _logger.LogDebug("Remote notifications registration completed: [{token}].", apnsToken);

        _messaging.SetApnsToken(apnsToken, ApnsTokenType.Unknown);
    }

    [Export("application:didFailToRegisterForRemoteNotificationsWithError:")]
    public void DidFailToRegisterForRemoteNotificationsWithError(UIApplication application, NSError error)
    {
        _logger.LogError("Remote notifications registration failed: {error}.", error?.LocalizedDescription);
    }

    [Export("messaging:didReceiveRegistrationToken:")]
    public void DidReceiveRegistrationToken(Messaging messaging, string apnsToken)
    {
        _logger.LogDebug("Remote notifications token received: [{token}].", apnsToken);

        if (!string.IsNullOrWhiteSpace(_settings.Current.DeviceId))
        {
            _logger.LogDebug("Remote notification subscribed to: [{deviceId}].", _settings.Current.DeviceId);

            messaging.Subscribe($"{_settings.Current.DeviceId}.call");
            messaging.Subscribe($"{_settings.Current.DeviceId}.motion");
        }
    }

    [Export("userNotificationCenter:willPresentNotification:withCompletionHandler:")]
    public void WillPresentNotification(UNUserNotificationCenter center, UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler)
    {
        var userInfo = notification.Request.Content.UserInfo;

        _logger.LogDebug("Remote notification received in foreground: [{notification}].", NSJsonSerialization.Serialize(userInfo, 0, out _).ToString());

        completionHandler(UNNotificationPresentationOptions.None);
    }

    [Export("application:didReceiveRemoteNotification:fetchCompletionHandler:")]
    public void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
    {
        _logger.LogDebug("Remote notification received in background: [{notification}].", NSJsonSerialization.Serialize(userInfo, 0, out _).ToString());

        completionHandler(UIBackgroundFetchResult.NewData);
    }

    [Export("userNotificationCenter:didReceiveNotificationResponse:withCompletionHandler:")]
    public void DidReceiveNotificationResponse(UNUserNotificationCenter center, UNNotificationResponse response, Action completionHandler)
    {
        var userInfo = response.Notification.Request.Content.UserInfo;

        _logger.LogDebug("Remote notification tapped: [{notification}].", NSJsonSerialization.Serialize(userInfo, 0, out _).ToString());

        //UNUserNotificationCenter.Current.RemoveAllDeliveredNotifications();

        completionHandler();
    }

    void OnSettingsChanged(ConnectionSettings previous, ConnectionSettings current)
    {
        if (!string.IsNullOrWhiteSpace(previous?.DeviceId))
        {
            _messaging.Unsubscribe($"{previous.DeviceId}.call");
            _messaging.Unsubscribe($"{previous.DeviceId}.motion");
        }

        if (!string.IsNullOrWhiteSpace(current?.DeviceId))
        {
            _logger.LogDebug("Remote notification subscribed to: [{deviceId}].", current.DeviceId);

            _messaging.Subscribe($"{current.DeviceId}.call");
            _messaging.Subscribe($"{current.DeviceId}.motion");
        }

        var userDefaults = new NSUserDefaults("group.com.anteater.intercom", NSUserDefaultsType.SuiteName);

        userDefaults.SetURL(new UriBuilder
        {
            Scheme = "http",
            UserName = current.Username,
            Password = current.Password,
            Host = current.Host,
            Port = current.WebPort,
            Path = "cgi-bin/images_cgi",
            Query = "channel=0",
        }.Uri, "snapshotUrl");

        userDefaults.Synchronize();
    }
}
