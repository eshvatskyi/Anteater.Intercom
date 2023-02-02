using Anteater.Intercom.Gui;
using Anteater.Intercom.Gui.ViewModels;
using Anteater.Intercom.Services.Audio;
using Anteater.Intercom.Services.Events;
using Anteater.Intercom.Services.ReversChannel;
using Anteater.Intercom.Services.Settings;
using Anteater.Intercom.Settings;
using CommunityToolkit.Maui;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Configuration;
using Sharp.UI;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace Anteater.Intercom;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var app = MauiApp
            .CreateBuilder()
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseSkiaSharp()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            .ConfigureSettings()
            .ConfigureServices()
            .ConfigureViewModels()
            .Build();

        return app;
    }

    static MauiAppBuilder ConfigureSettings(this MauiAppBuilder builder)
    {
        builder.Configuration.Sources.Clear();
        builder.Configuration.Add<SettingsConfigurationSource>(_ => { });

        builder.Services.Configure<ConnectionSettings>(builder.Configuration);

        return builder;
    }

    static MauiAppBuilder ConfigureServices(this MauiAppBuilder builder)
    {
        builder.Services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);

        builder.Services.AddSingleton<IAudioPlayback, AudioPlayback>();
        builder.Services.AddSingleton<IAudioRecord, AudioRecord>();

        builder.Services.AddSingleton<ReversChannelService>();
        builder.Services.AddSingleton<IReversAudioService>(x => x.GetRequiredService<ReversChannelService>());
        builder.Services.AddSingleton<IDoorLockService>(x => x.GetRequiredService<ReversChannelService>());

        builder.Services.AddHostedService<AlarmEventsService>();

        return builder;
    }

    static MauiAppBuilder ConfigureViewModels(this MauiAppBuilder builder)
    {
        builder.Services.AddTransient<Gui.Pages.Intercom>();
        builder.Services.AddTransient<Gui.Pages.Settings>();

        builder.Services.AddTransient<IntercomViewModel>();

        return builder;
    }
}
