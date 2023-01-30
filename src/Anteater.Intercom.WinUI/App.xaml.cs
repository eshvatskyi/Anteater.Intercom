global using Anteater.Intercom.Gui.Messages;

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Anteater.Intercom.Services;
using Anteater.Intercom.Services.Audio;
using Anteater.Intercom.Services.Events;
using Anteater.Intercom.Services.ReversChannel;
using Anteater.Intercom.Services.Settings;
using Anteater.Intercom.Settings;
using CommunityToolkit.Mvvm.Messaging;
using FFmpeg.AutoGen.Bindings.DynamicallyLoaded;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

namespace Anteater.Intercom;

sealed partial class App : Application
{
    private const string UniqueEventName = "{D2F9052F-CD44-44B7-8394-C91D8B0708F1}";
    private const string UniqueMutexName = "{DCF63EF4-F686-47C0-B5C8-36F94F35FE73}";

    public static IServiceProvider Services { get; private set; }

    private EventWaitHandle _eventWaitHandle;
    private Mutex _mutex;

    public App()
    {
        DynamicallyLoadedBindings.Initialize();

        var services = new ServiceCollection();

        ConfigureSettings(services);
        ConfigureServices(services);

        Services = services.BuildServiceProvider();

        UnhandledException += OnUnhandledException;

        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;

        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        InitializeComponent();
    }

    static void ConfigureSettings(IServiceCollection services)
    {
        var builder = new ConfigurationBuilder();

        builder.Sources.Clear();
        builder.Add<SettingsConfigurationSource>(_ => { });

        var config = builder.Build();

        services.AddSingleton<IConfiguration>(config);

        services.Configure<ConnectionSettings>(config);
    }

    void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);

        services.AddSingleton<IAudioPlayback, AudioPlayback>();
        services.AddSingleton<IAudioRecord, AudioRecord>();

        services.AddSingleton<ReversChannelService>();
        services.AddSingleton<IReversAudioService>(x => x.GetRequiredService<ReversChannelService>());
        services.AddSingleton<IDoorLockService>(x => x.GetRequiredService<ReversChannelService>());

        services.AddSingleton<UpdaterService>();
        services.AddSingleton<AlarmEventsService>();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs e)
    {
        base.OnLaunched(e);

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

            var updaterService = Services.GetRequiredService<UpdaterService>();
            var alarmService = Services.GetRequiredService<AlarmEventsService>();

            updaterService.Start();
            alarmService.Start();

            window.Closed += delegate
            {
                _ = updaterService.StopAsync();
                _ = alarmService.StopAsync();

                cts.Cancel();
            };

            return;
        }

        _eventWaitHandle.Set();

        Process.GetCurrentProcess().Kill();
    }

    void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        using var eventLog = new EventLog("Application") { Source = "Application" };

        eventLog.WriteEntry(e.Exception?.ToString() ?? "Unknown exception", EventLogEntryType.Error);
    }

    void OnDomainUnhandledException(object sender, System.UnhandledExceptionEventArgs e)
    {
        using var eventLog = new EventLog("Application") { Source = "Application" };

        eventLog.WriteEntry(e.ExceptionObject?.ToString() ?? "Unknown exception", EventLogEntryType.Error);
    }

    void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        using var eventLog = new EventLog("Application") { Source = "Application" };

        eventLog.WriteEntry(e.Exception?.ToString() ?? "Unknown exception", EventLogEntryType.Error);
    }
}
