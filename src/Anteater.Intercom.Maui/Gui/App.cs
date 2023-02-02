using Anteater.Intercom.Gui.Messages;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
using Page = Microsoft.Maui.Controls.Page;

namespace Anteater.Intercom.Gui;

using Sharp.UI;

public class App : Application
{
    private readonly IMessenger _messenger;
    private readonly IEnumerable<IHostedService> _hostedServices;

    public App(IMessenger messenger, IEnumerable<IHostedService> hostedServices, Pages.Intercom intercomPage)
    {
        _messenger = messenger;
        _hostedServices = hostedServices;


        MainPage = new NavigationPage(x => x
            .Resources(new ResourceDictionary
            {
                new Style<NavigationPage>(applyToDerivedTypes: true)
                {
                    NavigationPage.BarBackgroundColorProperty.Set(Colors.Black),
                    NavigationPage.BarTextColorProperty.Set(Colors.White),
                    NavigationPage.IconColorProperty.Set(Colors.White),
                },
                new Style<Page>(applyToDerivedTypes: true)
                {
                    Page.PaddingProperty.Set(0),
                    Page.BackgroundColorProperty.Set(Colors.Black),
                }
            })
            .On<iOS>().EnableTranslucentNavigationBar()
        )
        {
            intercomPage
        };
    }

    protected override Window CreateWindow(IActivationState activationState)
    {
        var window = base.CreateWindow(activationState);

        var stoppingTokenSource = new CancellationTokenSource();

        window.Created += delegate
        {
            foreach (var service in _hostedServices)
            {
                _ = service.StartAsync(stoppingTokenSource.Token);
            }
        };

        window.Destroying += delegate
        {
            _messenger.Send(new WindowStateChanged(WindowState.Closing));

            stoppingTokenSource.Cancel();
        };


        window.Stopped += delegate
        {
            _messenger.Send(new WindowStateChanged(WindowState.Stopped));
        };

        window.Resumed += delegate
        {
            _messenger.Send(new WindowStateChanged(WindowState.Resumed));
        };

        return window;
    }
}
