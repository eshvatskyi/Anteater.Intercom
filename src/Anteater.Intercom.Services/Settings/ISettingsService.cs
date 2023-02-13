namespace Anteater.Intercom.Services.Settings;

public interface ISettingsService
{
    public delegate void SettingsChanged(ConnectionSettings previous, ConnectionSettings current);

    event SettingsChanged Changed;

    ConnectionSettings Current { get; }

    void Refresh();
}
