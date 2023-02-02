using Anteater.Intercom.Gui.Behaviors;
using Anteater.Intercom.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Maui.Layouts;

namespace Anteater.Intercom.Gui.Pages;

using Anteater.Intercom.Gui.Controls;
using Sharp.UI;

public class Settings : ContentPage
{
    private readonly IConfiguration _configuration;

    public Settings(IConfiguration configuration)
    {
        _configuration = configuration;

        Title = "Settings";

        NavigationPage.SetHasNavigationBar(this, true);

        Content = new AbsoluteLayout()
        {
            new VerticalStackLayout(x => x
                .Padding(new Thickness(20, 10))
                .Spacing(10)
                .AbsoluteLayoutBounds(new Rect(0, 0, 1, 1))
                .AbsoluteLayoutFlags(AbsoluteLayoutFlags.PositionProportional | AbsoluteLayoutFlags.SizeProportional)
            )
            {
                new SettingEntry()
                    .Placeholder("Host")
                    .Key("Host")
                    .ReturnType(ReturnType.Next),
                new SettingEntry()
                    .Placeholder("Username")
                    .Key("Username")
                    .ReturnType(ReturnType.Next),
                new SettingEntry()
                    .Placeholder("Password")
                    .Key("Password")
                    .IsPassword(true)
                    .ReturnType(ReturnType.Next),
                new SettingEntry()
                    .Placeholder("WebPort")
                    .Key("WebPort")
                    .Default("80")
                    .Keyboard(Keyboard.Numeric)
                    .ReturnType(ReturnType.Next),
                new SettingEntry()
                    .Placeholder("RtspPort")
                    .Key("RtspPort")
                    .Default("554")
                    .Keyboard(Keyboard.Numeric)
                    .ReturnType(ReturnType.Next),
                new SettingEntry()
                    .Placeholder("DataPort")
                    .Key("DataPort")
                    .Default("5000")
                    .Keyboard(Keyboard.Numeric)
                    .ReturnType(ReturnType.Next),
            }
        };

        Resources = new ResourceDictionary
        {
            new Style<SettingEntry>
            {
                SettingBehavior.AttachedProperty.Set(true)
            }
        };
    }

    protected override void OnDisappearing()
    {
        (_configuration as IConfigurationRoot).Providers
            .OfType<SettingsConfigurationProvider>()
            .First()
            .Load();

        base.OnDisappearing();
    }
}
