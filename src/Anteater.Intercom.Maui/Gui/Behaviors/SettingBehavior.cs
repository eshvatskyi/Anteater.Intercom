using Anteater.Intercom.Gui.Controls;
using Sharp.UI;

namespace Anteater.Intercom.Gui.Behaviors;

public class SettingBehavior : AttachedBehavior<SettingEntry, SettingBehavior>
{
    protected override void OnAttachedTo(SettingEntry entry)
    {
        if (string.IsNullOrEmpty(entry.Key))
        {
            return;
        }

        entry.Unfocused += SettingChanged;

        if (Keyboard.Numeric.Equals(entry.Keyboard))
        {
            var defaultValue = int.TryParse(entry.Default, out var value) ? value : 0;

            entry.Text = Preferences.Default.Get(entry.Key, defaultValue).ToString();
        }
        else
        {
            entry.Text = Preferences.Default.Get(entry.Key, entry.Default);
        }

        base.OnAttachedTo(entry);
    }

    protected override void OnDetachingFrom(SettingEntry entry)
    {
        entry.Unfocused -= SettingChanged;

        base.OnDetachingFrom(entry);
    }

    void SettingChanged(object sender, FocusEventArgs e)
    {
        if (sender is not SettingEntry entry)
        {
            return;
        }

        if (Keyboard.Numeric.Equals(entry.Keyboard))
        {
            var defaultValue = int.TryParse(entry.Default, out var value) ? value : 0;

            var currentValue = int.TryParse(entry.Text?.Trim(), out value) ? value : defaultValue;

            Preferences.Default.Set(entry.Key, currentValue);
        }
        else
        {
            Preferences.Default.Set(entry.Key, entry.Text?.Trim() ?? "");
        }
    }
}
