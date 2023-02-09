using Anteater.Intercom.Features.Intercom;
using Anteater.Intercom.Messages;
using CommunityToolkit.Maui.Markup;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
using Application = Microsoft.Maui.Controls.Application;
using NavigationPage = Microsoft.Maui.Controls.NavigationPage;
using Page = Microsoft.Maui.Controls.Page;

namespace Anteater.Intercom;

public class App : Application
{
    private readonly IMessenger _messenger;
    private readonly IEnumerable<IHostedService> _hostedServices;
    private readonly CancellationTokenSource _stoppingTokenSource = new();

    public App(IMessenger messenger, IEnumerable<IHostedService> hostedServices, IntercomPage intercomPage)
    {
        _messenger = messenger;
        _hostedServices = hostedServices;

        MainPage = new NavigationPage(intercomPage)
        {
            Resources = new ResourceDictionary
            {
                new Style<NavigationPage>(
                    (NavigationPage.BarBackgroundColorProperty, Colors.Black),
                    (NavigationPage.BarTextColorProperty, Colors.White),
                    (NavigationPage.IconColorProperty, Colors.White)
                )
                .ApplyToDerivedTypes(true),

                new Style<Page>(
                    (Page.PaddingProperty, 0),
                    (Microsoft.Maui.Controls.VisualElement.BackgroundColorProperty, Colors.Black)
                )
                .ApplyToDerivedTypes(true),
            },
        }.Invoke(x => x.On<iOS>().EnableTranslucentNavigationBar());
    }

    protected override Window CreateWindow(IActivationState activationState)
    {
        var window = base.CreateWindow(activationState);

        window.Created += OnWindowCreated;
        window.Destroying += OnWindowsDestroying;
        window.Stopped += OnWindowStopped;
        window.Resumed += OnWindowResumed;

        return window;
    }

    void OnWindowCreated(object sender, EventArgs e)
    {
        foreach (var service in _hostedServices)
        {
            _ = service.StartAsync(_stoppingTokenSource.Token);
        }
    }

    void OnWindowsDestroying(object sender, EventArgs e)
    {
        _messenger.Send(new WindowStateChanged(WindowState.Closing));

        _stoppingTokenSource.Cancel();
    }

    void OnWindowStopped(object sender, EventArgs e)
    {
        _messenger.Send(new WindowStateChanged(WindowState.Stopped));
    }

    void OnWindowResumed(object sender, EventArgs e)
    {
        _messenger.Send(new WindowStateChanged(WindowState.Resumed));
    }
}
