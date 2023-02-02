using System;
using Anteater.Intercom.Services;
using Anteater.Intercom.Services.Audio;
using Anteater.Intercom.Services.Events;
using Anteater.Intercom.Services.ReversChannel;
using Anteater.Intercom.Services.Settings;
using Anteater.Intercom.Settings;
using CommunityToolkit.Extensions.Hosting;
using CommunityToolkit.Mvvm.Messaging;
using FFmpeg.AutoGen.Bindings.DynamicallyLoaded;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Anteater.Intercom;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        DynamicallyLoadedBindings.Initialize();

        new WindowsAppSdkHostBuilder<App>()
            .ConfigureAppConfiguration(builder =>
            {
                builder.Sources.Clear();
                builder.Add<SettingsConfigurationSource>(_ => { });
            })
            .ConfigureServices((context, services) =>
            {
                services.Configure<ConnectionSettings>(context.Configuration);

                services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);

                services.AddSingleton<IAudioPlayback, AudioPlayback>();
                services.AddSingleton<IAudioRecord, AudioRecord>();

                services.AddSingleton<ReversChannelService>();
                services.AddSingleton<IReversAudioService>(x => x.GetRequiredService<ReversChannelService>());
                services.AddSingleton<IDoorLockService>(x => x.GetRequiredService<ReversChannelService>());

                services.AddHostedService<AlarmEventsService>();
                services.AddHostedService<UpdaterService>();

                services.AddSingleton<MainWindow>();
            })
            .Build()
            .Start();
    }
}
