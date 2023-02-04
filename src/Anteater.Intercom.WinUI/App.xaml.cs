global using Anteater.Intercom.Gui.Messages;

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

namespace Anteater.Intercom;

sealed partial class App : CancelableApplication
{
    private const string UniqueEventName = "{D2F9052F-CD44-44B7-8394-C91D8B0708F1}";
    private const string UniqueMutexName = "{DCF63EF4-F686-47C0-B5C8-36F94F35FE73}";

    private EventWaitHandle _eventWaitHandle;
    private Mutex _mutex;

    public App()
    {
        UnhandledException += OnUnhandledException;

        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;

        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs e)
    {
        base.OnLaunched(e);

        _mutex = new Mutex(true, UniqueMutexName, out var isOwned);
        _eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, UniqueEventName);

        GC.KeepAlive(_mutex);

        if (isOwned)
        {
            var window = Services.GetRequiredService<MainWindow>();

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

            window.Closed += (_, _) =>
            {
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
