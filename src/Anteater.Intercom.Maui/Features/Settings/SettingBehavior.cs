namespace Anteater.Intercom.Features.Settings;

public class SettingBehavior
{
    public static readonly BindableProperty AttachBehaviorProperty = BindableProperty
        .CreateAttached("AttachBehavior", typeof(bool), typeof(SettingBehavior), false, propertyChanged: OnAttachBehaviorChanged);

    public static bool GetAttachBehavior(BindableObject view)
    {
        return (bool)view.GetValue(AttachBehaviorProperty);
    }

    public static void SetAttachBehavior(BindableObject view, bool value)
    {
        view.SetValue(AttachBehaviorProperty, value);
    }

    static void OnAttachBehaviorChanged(BindableObject view, object oldValue, object newValue)
    {
        if (view is not SettingEntry entry)
        {
            return;
        }

        if (string.IsNullOrEmpty(entry.Key))
        {
            return;
        }

        if (newValue is bool attachBehavior && attachBehavior)
        {
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
        }
        else
        {
            entry.Unfocused -= SettingChanged;
        }
    }

    static void SettingChanged(object sender, FocusEventArgs e)
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
