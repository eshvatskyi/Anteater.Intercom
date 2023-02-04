using Foundation;
using Microsoft.Maui.LifecycleEvents;
using Plugin.Firebase.CloudMessaging;

namespace Anteater.Intercom;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp(ConfigurePlatformServices);

    private MauiAppBuilder ConfigurePlatformServices(MauiAppBuilder builder)
    {
        builder.ConfigureLifecycleEvents(events =>
        {
            events.AddiOS(x => x.FinishedLaunching((app, opts) =>
            {
                Plugin.Firebase.iOS.CrossFirebase.Initialize(app, opts, new(
                    isCloudMessagingEnabled: true
                ));

                return true;
            }));
        });

        builder.Services.AddSingleton(_ => CrossFirebaseCloudMessaging.Current);

        builder.Services.AddHostedService<PushNotificationService>();

        return builder;
    }
}
