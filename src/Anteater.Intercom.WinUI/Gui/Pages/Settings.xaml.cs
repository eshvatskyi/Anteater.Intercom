using System;
using System.Linq;
using Anteater.Intercom.Services.Settings;
using Anteater.Intercom.Settings;
using CommunityToolkit.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Anteater.Intercom.Gui.Pages;

sealed partial class Settings : Page
{
    private readonly IConfiguration _configuration;
    private readonly IOptionsMonitor<ConnectionSettings> _connectionSettings;

    public Settings()
    {
        _configuration = (App.Current as CancelableApplication).Services.GetRequiredService<IConfiguration>();
        _connectionSettings = (App.Current as CancelableApplication).Services.GetService<IOptionsMonitor<ConnectionSettings>>();

        InitializeComponent();

        _host.Text = _connectionSettings.CurrentValue.Host;
        _username.Text = _connectionSettings.CurrentValue.Username;
        _password.Text = _connectionSettings.CurrentValue.Password;
        _webPort.Value = _connectionSettings.CurrentValue.WebPort;
        _rtspPort.Value = _connectionSettings.CurrentValue.RtspPort;
        _dataPort.Value = _connectionSettings.CurrentValue.DataPort;
    }

    void Button_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        try
        {
            var settings = new ConnectionSettings
            {
                Host = _host.Text.Trim(),
                Username = _username.Text.Trim(),
                Password = _password.Text.Trim(),
                WebPort = (int)_webPort.Value,
                RtspPort = (int)_rtspPort.Value,
                DataPort = (int)_dataPort.Value,
            };

            (_configuration as IConfigurationRoot).Providers
                .OfType<SettingsConfigurationProvider>()
                .First()
                .Write(settings);

            MainWindow.Instance.NavigateToType(typeof(Intercom), false);
        }
        catch (Exception ex)
        {
            var alert = new Popup();
            alert.Child = new TextBlock() { Text = $"Failed to write settings:\r\n{ex}" };
            alert.IsOpen = true;
        }
    }
}
