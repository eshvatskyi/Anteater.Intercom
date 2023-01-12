using Microsoft.Extensions.Configuration;

namespace Anteater.Intercom.Settings;

public class SettingsConfigurationSource : IConfigurationSource
{
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new SettingsConfigurationProvider();
    }
}
