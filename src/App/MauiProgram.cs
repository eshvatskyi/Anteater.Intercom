using Anteater.Intercom.Features.Intercom;
using Anteater.Intercom.Features.Settings;
using Anteater.Intercom.Core;
using Anteater.Intercom.Core.Audio;
using Anteater.Intercom.Core.Events;
using Anteater.Intercom.Core.ReversChannel;
using Anteater.Intercom.Core.Settings;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Markup;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace Anteater.Intercom;

public static partial class MauiProgram
{
    public static MauiApp CreateMauiApp(Func<MauiAppBuilder, MauiAppBuilder> configurePlatformServices = null)
    {
        var builder = MauiApp
            .CreateBuilder()
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseMauiCommunityToolkitMarkup()
            .UseSkiaSharp()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            .ConfigureLogging()
            .ConfigureServices()
            .ConfigureViews()
            .ConfigureViewModels();

        configurePlatformServices?.Invoke(builder);

        var app = builder.Build();

        return app;
    }

    static MauiAppBuilder ConfigureServices(this MauiAppBuilder builder)
    {
        builder.Services.AddSingleton<ISettingsProvider, SettingsProvider>();
        builder.Services.AddSingleton<ISettingsService, SettingsService>();

        builder.Services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);

        builder.Services.AddSingleton<IAudioPlayback, AudioPlayback>();
        builder.Services.AddSingleton<IAudioRecord, AudioRecord>();

        builder.Services.AddSingleton<ReversChannelService>();
        builder.Services.AddHostedService(x => x.GetRequiredService<ReversChannelService>());
        builder.Services.AddSingleton<IReversAudioService>(x => x.GetRequiredService<ReversChannelService>());
        builder.Services.AddSingleton<IDoorLockService>(x => x.GetRequiredService<ReversChannelService>());

        builder.Services.AddHostedService<AlarmEventsService>();

        return builder;
    }

    static MauiAppBuilder ConfigureLogging(this MauiAppBuilder builder)
    {
        builder.Services.AddLogging(logging =>
        {
            logging.ClearProviders();

#if WINDOWS
            logging.AddDebug();
#else
            logging.AddConsole(opts => opts.FormatterName = nameof(ConsoleLogFormatter));
            logging.AddConsoleFormatter<ConsoleLogFormatter, ConsoleFormatterOptions>(opts =>
            {
                opts.IncludeScopes = false;
            });
#endif

#if DEBUG
            logging.SetMinimumLevel(LogLevel.Debug);
#else
            logging.SetMinimumLevel(LogLevel.Information);
#endif
        });

        return builder;
    }

    static MauiAppBuilder ConfigureViews(this MauiAppBuilder builder)
    {
        builder.Services.AddSingleton<IntercomPage>();
        builder.Services.AddSingleton<SettingsPage>();

        return builder;
    }

    static MauiAppBuilder ConfigureViewModels(this MauiAppBuilder builder)
    {
        builder.Services.AddTransient<PlayerViewModel>();
        builder.Services.AddTransient<DoorViewModel>();
        builder.Services.AddTransient<CallViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<IntercomViewModel>();

        return builder;
    }
}
