using Anteater.Intercom.Settings;
using Microsoft.Extensions.Configuration;

namespace Anteater.Intercom.Gui;

public partial class Settings : ContentPage
{
    private readonly IConfiguration _configuration;

    public Settings()
    {
        _configuration = App.Services.GetRequiredService<IConfiguration>();

        InitializeComponent();
    }

    protected override void OnDisappearing()
    {
        (_configuration as IConfigurationRoot).Providers
            .OfType<SettingsConfigurationProvider>()
            .First()
            .Load();

        base.OnDisappearing();
    }
}
