using Anteater.Intercom.Services.Settings;
using Microsoft.Extensions.Configuration;

namespace Anteater.Intercom.Settings;

public class SettingsConfigurationProvider : ConfigurationProvider
{
    public override void Load()
    {
        var defaultSettings = new ConnectionSettings();

        var oldSettings = new ConnectionSettings
        {
            Host = Data.TryGetValue(nameof(ConnectionSettings.Host), out var host) ? host : defaultSettings.Host,
            Username = Data.TryGetValue(nameof(ConnectionSettings.Username), out var username) ? username : defaultSettings.Username,
            Password = Data.TryGetValue(nameof(ConnectionSettings.Password), out var password) ? password : defaultSettings.Password,
            WebPort = Data.TryGetValue(nameof(ConnectionSettings.WebPort), out var webPort) ? Convert.ToInt32(webPort) : defaultSettings.WebPort,
            RtspPort = Data.TryGetValue(nameof(ConnectionSettings.RtspPort), out var rtspPort) ? Convert.ToInt32(rtspPort) : defaultSettings.RtspPort,
            DataPort = Data.TryGetValue(nameof(ConnectionSettings.DataPort), out var dataPort) ? Convert.ToInt32(dataPort) : defaultSettings.DataPort,
            DeviceId = Data.TryGetValue(nameof(ConnectionSettings.DeviceId), out var deviceId) ? deviceId : defaultSettings.DeviceId,
        };

        var newSettings = new ConnectionSettings
        {
            Host = Preferences.Default.Get(nameof(ConnectionSettings.Host), defaultSettings.Host),
            Username = Preferences.Default.Get(nameof(ConnectionSettings.Username), defaultSettings.Username),
            Password = Preferences.Default.Get(nameof(ConnectionSettings.Password), defaultSettings.Password),
            WebPort = Preferences.Default.Get(nameof(ConnectionSettings.WebPort), defaultSettings.WebPort),
            RtspPort = Preferences.Default.Get(nameof(ConnectionSettings.RtspPort), defaultSettings.RtspPort),
            DataPort = Preferences.Default.Get(nameof(ConnectionSettings.DataPort), defaultSettings.DataPort),
            DeviceId = Preferences.Default.Get(nameof(ConnectionSettings.DeviceId), defaultSettings.DeviceId),
        };

        Data.Clear();

        Data.Add(nameof(ConnectionSettings.Host), newSettings.Host);
        Data.Add(nameof(ConnectionSettings.Username), newSettings.Username);
        Data.Add(nameof(ConnectionSettings.Password), newSettings.Password);
        Data.Add(nameof(ConnectionSettings.WebPort), newSettings.WebPort.ToString());
        Data.Add(nameof(ConnectionSettings.RtspPort), newSettings.RtspPort.ToString());
        Data.Add(nameof(ConnectionSettings.DataPort), newSettings.DataPort.ToString());
        Data.Add(nameof(ConnectionSettings.DeviceId), newSettings.DeviceId.ToString());

        if (oldSettings != newSettings)
        {
            OnReload();
        }
    }
}
