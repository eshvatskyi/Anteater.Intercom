using Anteater.Intercom.Core.Settings;

namespace Anteater.Intercom;

public class SettingsProvider : ISettingsProvider
{
    private static readonly ConnectionSettings DefaultSettings = new();

    public ConnectionSettings Get()
    {
        var settings = new ConnectionSettings
        {
            Host = Preferences.Default.Get(nameof(ConnectionSettings.Host), DefaultSettings.Host),
            Username = Preferences.Default.Get(nameof(ConnectionSettings.Username), DefaultSettings.Username),
            Password = Preferences.Default.Get(nameof(ConnectionSettings.Password), DefaultSettings.Password),
            WebPort = Preferences.Default.Get(nameof(ConnectionSettings.WebPort), DefaultSettings.WebPort),
            RtspPort = Preferences.Default.Get(nameof(ConnectionSettings.RtspPort), DefaultSettings.RtspPort),
            DataPort = Preferences.Default.Get(nameof(ConnectionSettings.DataPort), DefaultSettings.DataPort),
            DeviceId = Preferences.Default.Get(nameof(ConnectionSettings.DeviceId), DefaultSettings.DeviceId),
        };

        return settings;
    }

    public T Get<T>(string name) where T : IConvertible
    {
        var property = typeof(ConnectionSettings).GetProperty(name);
        if (property is null)
        {
            throw new ArgumentException($"Invalid settings property: {name}");
        }

        var defaultValue = property.GetMethod.Invoke(DefaultSettings, null);

        return Preferences.Default.Get(name, (T)defaultValue);
    }

    public void Set<T>(string name, T value) where T : IConvertible
    {
        var property = typeof(ConnectionSettings).GetProperty(name);
        if (property is null)
        {
            throw new ArgumentException($"Invalid settings property: {name}");
        }

        var defaultValue = property.GetMethod.Invoke(DefaultSettings, null);

        Preferences.Default.Set(name, Comparer<T>.Default.Compare(value, default) == 0 ? (T)defaultValue : value);
    }
}
