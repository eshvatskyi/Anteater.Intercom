namespace Anteater.Intercom.Services.Settings;

public interface ISettingsProvider
{
    ConnectionSettings Get();

    T Get<T>(string name) where T : IConvertible;

    void Set<T>(string name, T value) where T: IConvertible;
}
