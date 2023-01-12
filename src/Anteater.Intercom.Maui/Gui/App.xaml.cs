using Anteater.Intercom.Gui.Messages;
using Anteater.Intercom.Services;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
using Application = Microsoft.Maui.Controls.Application;
using NavigationPage = Microsoft.Maui.Controls.NavigationPage;

namespace Anteater.Intercom.Gui;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; }

    public App(IServiceProvider services)
    {
        Services = services;

        InitializeComponent();

        var page = new NavigationPage(new MainPage());

        page.On<iOS>().EnableTranslucentNavigationBar();

        MainPage = page;
    }

    protected override Window CreateWindow(IActivationState activationState)
    {
        var window = base.CreateWindow(activationState);

        var backgroundServices = Services.GetServices<BackgroundService>();

        window.Created += delegate
        {
            foreach (var service in backgroundServices)
            {
                service.Start();
            }
        };

        window.Destroying += delegate
        {
            foreach (var service in backgroundServices)
            {
                _ = service.StopAsync();
            }
        };

        var messenger = Services.GetRequiredService<IMessenger>();

        window.Stopped += delegate
        {
            messenger.Send(new WindowStateChanged(WindowState.Stopped));
        };

        window.Resumed += delegate
        {
            messenger.Send(new WindowStateChanged(WindowState.Resumed));
        };

        return window;
    }
}
