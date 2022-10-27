global using Anteater.Intercom.Gui.Communication;
global using Anteater.Intercom.Gui.Helpers;

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Anteater.Intercom.Device;
using Anteater.Intercom.Device.Audio;
using Anteater.Intercom.Device.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Anteater.Intercom;

sealed partial class App : Application
{
    private const string UniqueEventName = "{D2F9052F-CD44-44B7-8394-C91D8B0708F1}";
    private const string UniqueMutexName = "{DCF63EF4-F686-47C0-B5C8-36F94F35FE73}";

    public static IServiceProvider ServiceProvider { get; private set; }

    private EventWaitHandle _eventWaitHandle;
    private Mutex _mutex;

    public App()
    {
        var services = new ServiceCollection();

        ConfigureServices(services);

        ServiceProvider = services.BuildServiceProvider();

        UnhandledException += OnUnhandledException;

        InitializeComponent();
    }

    void ConfigureServices(IServiceCollection services)
    {
        var builder = new ConfigurationBuilder()
            .AddIniFile("App.ini", optional: true, reloadOnChange: true);

        var config = builder.Build();

        services.AddSingleton<IConfiguration>(config);

        services.AddOptions();

        services.Configure<ConnectionSettings>(config);

        services.AddAnteaterPipe();

        services.AddSingleton<AlarmEventsService>();
        services.AddSingleton<BackChannelConnection>();
    }

    void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        var alert = new Popup();
        alert.Child = new TextBlock() { Text = e.Exception.ToString() };
        alert.IsOpen = true;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        _mutex = new Mutex(true, UniqueMutexName, out var isOwned);
        _eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, UniqueEventName);

        GC.KeepAlive(_mutex);

        if (isOwned)
        {
            var window = new MainWindow();

            var cts = new CancellationTokenSource();

            Task.Run(() =>
            {
                while (!cts.IsCancellationRequested)
                {
                    if (_eventWaitHandle.WaitOne(500, false))
                    {
                        MainWindow.Instance.BringToForeground();
                    }
                }
            }, cts.Token);

            var alarmService = ServiceProvider.GetRequiredService<AlarmEventsService>();

            //alarmService.Start();

            window.Closed += delegate
            {
                _ = alarmService.StopAsync();

                cts.Cancel();
            };

            return;
        }

        _eventWaitHandle.Set();

        Process.GetCurrentProcess().Kill();
    }
}
