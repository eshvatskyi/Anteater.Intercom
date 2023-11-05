namespace Anteater.Intercom.Features.Settings;

public class SettingEntry : Entry
{
    public static readonly BindableProperty KeyProperty = BindableProperty
        .Create(nameof(Key), typeof(string), typeof(SettingEntry), null);

    public string Key
    {
        get => GetValue(KeyProperty) as string;
        set => SetValue(KeyProperty, value);
    }
}

public static class SettingsEntryExtensions
{
    public static SettingEntry Key(this SettingEntry entry, string value)
    {
        entry.Key = value;
        return entry;
    }
}

