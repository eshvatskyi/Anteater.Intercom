using Anteater.Intercom.Gui.Messages;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
using Page = Microsoft.Maui.Controls.Page;

namespace Anteater.Intercom.Gui;

using System;
using Sharp.UI;

public class App : Application
{
    private readonly IMessenger _messenger;
    private readonly IEnumerable<IHostedService> _hostedServices;
    private readonly CancellationTokenSource _stoppingTokenSource = new();

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
