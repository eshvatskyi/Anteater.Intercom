namespace Anteater.Intercom.Services.Settings;

public class SettingsService : ISettingsService
{
    private readonly ISettingsProvider _settingsProvider;

    private ConnectionSettings _settings;

    public SettingsService(ISettingsProvider settingsProvider)
    {
        _settingsProvider = settingsProvider;

        _settings = _settingsProvider.Get();
    }

    public event ISettingsService.SettingsChanged Changed;

    public ConnectionSettings Current
    {
        get
        {
            lock (this)
            {
                return _settings;
            }
        }
    }

    public void Refresh()
    {
        lock (this)
        {
            var settings = _settings;

            _settings = _settingsProvider.Get();

            if (_settings != settings)
            {
                Changed?.Invoke(settings, _settings);
            }
        }
    }
}
