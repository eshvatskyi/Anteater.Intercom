using FFmpeg.AutoGen.Bindings.DynamicallyLoaded;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Maui.LifecycleEvents;
using Microsoft.UI;
using Microsoft.UI.Windowing;

namespace Anteater.Intercom.WinUI;

public partial class App : MauiWinUIApplication
{
    public App()
    {
        DynamicallyLoadedBindings.Initialize();

        InitializeComponent();
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp(ConfigurePlatformServices);

    MauiAppBuilder ConfigurePlatformServices(MauiAppBuilder builder)
    {
        builder.ConfigureLifecycleEvents(events =>
        {
            events.AddWindows(x => x.OnWindowCreated(window =>
            {
                var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
                var win32WindowsId = Win32Interop.GetWindowIdFromWindow(handle);
                var appWindow = AppWindow.GetFromWindowId(win32WindowsId);
                var displayArea = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Primary);

                void SwitchWindowFullScreenState()
                {
                    if (appWindow.Presenter.Kind == AppWindowPresenterKind.FullScreen)
                    {
                        window.ExtendsContentIntoTitleBar = false;

                        appWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
                        appWindow.Resize(new() { Width = 800, Height = 600 });
                        appWindow.Move(new() { X = (displayArea.WorkArea.Width - 800) / 2, Y = (displayArea.WorkArea.Height - 600) / 2 });                        
                    }
                    else
                    {
                        window.ExtendsContentIntoTitleBar = true;

                        appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
                    }
                }

                events.AddEvent("WindowFullScreenSwitchRequested", SwitchWindowFullScreenState);

                SwitchWindowFullScreenState();
            }));
        });

        builder.Services.AddSingleton(x => FirebaseApp.Create(new AppOptions
        {
            Credential = GoogleCredential.FromFile("GoogleService-Info.json"),
        }));

        builder.Services.AddSingleton(x => FirebaseMessaging.GetMessaging(x.GetRequiredService<FirebaseApp>()));

        builder.Services.AddHostedService<PushNotificationService>();

        return builder;
    }
}

