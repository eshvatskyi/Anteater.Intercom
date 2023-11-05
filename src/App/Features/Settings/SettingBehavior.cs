using Anteater.Intercom.Core.Settings;

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

        var settings = App.Services.GetRequiredService<ISettingsProvider>();

        if (newValue is bool attachBehavior && attachBehavior)
        {
            entry.Unfocused += SettingChanged;

            if (Keyboard.Numeric.Equals(entry.Keyboard))
            {
                entry.Text = settings.Get<int>(entry.Key).ToString();
            }
            else
            {
                entry.Text = settings.Get<string>(entry.Key);
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

        var settings = App.Services.GetRequiredService<ISettingsProvider>();

        if (Keyboard.Numeric.Equals(entry.Keyboard))
        {
            settings.Set(entry.Key, int.TryParse(entry.Text?.Trim(), out var value) ? value : default);

            entry.Text = settings.Get<int>(entry.Key).ToString();
        }
        else
        {
            settings.Set(entry.Key, entry.Text?.Trim() ?? "");

            entry.Text = settings.Get<string>(entry.Key);
        }
    }
}
