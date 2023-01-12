namespace Anteater.Intercom.Gui.Behaviors;

public class SettingBehavior : Behavior<Entry>
{
    public string Key { get; set; }

    public string Type { get; set; }

    public string Default { get; set; }

    protected override void OnAttachedTo(Entry entry)
    {
        if (string.IsNullOrEmpty(Key))
        {
            return;
        }

        entry.Unfocused += SettingChanged;

        switch (Type)
        {
            case "int":
                entry.Text = Preferences.Default.Get(
                    Key,
                    int.TryParse(Default, out var defaultValue) ? defaultValue : 0)
                    .ToString();
                break;

            default:
                entry.Text = Preferences.Default.Get(Key, "");
                break;
        }

        base.OnAttachedTo(entry);
    }

    protected override void OnDetachingFrom(Entry entry)
    {
        entry.Unfocused -= SettingChanged;

        base.OnDetachingFrom(entry);
    }

    void SettingChanged(object sender, FocusEventArgs e)
    {
        if (sender is not Entry entry)
        {
            return;
        }

        switch (Type)
        {
            case "int":
                Preferences.Default.Set(
                    Key,
                    int.TryParse(entry.Text?.Trim(), out var value)
                        ? value
                        : int.TryParse(Default, out var defaultValue)
                            ? defaultValue
                            : 0);
                break;

            default:
                Preferences.Default.Set(Key, entry.Text?.Trim() ?? "");
                break;
        }
    }
}
