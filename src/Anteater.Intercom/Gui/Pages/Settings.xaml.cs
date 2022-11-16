using System;
using System.IO;
using System.Text;
using Anteater.Intercom.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Squirrel;

namespace Anteater.Intercom.Gui.Pages;

sealed partial class Settings : Page
{
    private readonly IOptionsMonitor<ConnectionSettings> _connectionSettings;

    public Settings()
    {
        _connectionSettings = App.Services.GetService<IOptionsMonitor<ConnectionSettings>>();

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
            var config = new StringBuilder();

            config.AppendLine($"Host={_host.Text.Trim()}");
            config.AppendLine($"Username={_username.Text.Trim()}");
            config.AppendLine($"Password={_password.Text.Trim()}");
            config.AppendLine($"WebPort={_webPort.Value}");
            config.AppendLine($"RtspPort={_rtspPort.Value}");
            config.AppendLine($"DataPort={_dataPort.Value}");

            using var mgr = new UpdateManager("");

            using var configFile = new FileStream($"{(mgr.IsInstalledApp ? mgr.AppDirectory + "/" : "")}App.ini", FileMode.Create, FileAccess.Write);

            configFile.Write(Encoding.UTF8.GetBytes(config.ToString()));
            configFile.Close();

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
