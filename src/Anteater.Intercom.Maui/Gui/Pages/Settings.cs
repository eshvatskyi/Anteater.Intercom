using System.Net.Http.Headers;
using System.Text;
using Anteater.Intercom.Gui.Behaviors;
using Anteater.Intercom.Gui.Controls;
using Anteater.Intercom.Services.Settings;
using Anteater.Intercom.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Maui.Layouts;

namespace Anteater.Intercom.Gui.Pages;

using Sharp.UI;

public class Settings : ContentPage
{
    private readonly IConfiguration _configuration;

    public Settings(IConfiguration configuration)
    {
        _configuration = configuration;

        NavigationPage.SetHasNavigationBar(this, false);

        Build();
    }

    private void Build()
    {
        Content = new AbsoluteLayout()
        {
            new AbsoluteLayout(x => x
                .Padding(new Thickness(20, 10))
                .AbsoluteLayoutBounds(new Rect(0, 0, 1, 40))
                .AbsoluteLayoutFlags(AbsoluteLayoutFlags.PositionProportional | AbsoluteLayoutFlags.WidthProportional))
            {
                new Button()
                    .Text("Back")
                    .TextColor(Colors.White)
                    .OnClicked(_ => SendBackButtonPressed())
                    .AbsoluteLayoutBounds(new Rect(0, 0.5, -1, -1))
                    .AbsoluteLayoutFlags(AbsoluteLayoutFlags.PositionProportional),
                new Label()
                    .Text("Settings")
                    .TextColor(Colors.White)
                    .FontAttributes(FontAttributes.Bold)
                    .FontSize(18)
                    .AbsoluteLayoutBounds(new Rect(0.5, 0.5, -1, -1))
                    .AbsoluteLayoutFlags(AbsoluteLayoutFlags.PositionProportional),
            },
            new VerticalStackLayout(x => x
                .Padding(new Thickness(20, 10))
                .Spacing(10)
                .AbsoluteLayoutBounds(new Rect(0, 40, 1, 1))
                .AbsoluteLayoutFlags(AbsoluteLayoutFlags.XProportional | AbsoluteLayoutFlags.SizeProportional)
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
        }.Resources(new ResourceDictionary
        {
            new Style<SettingEntry>
            {
                SettingBehavior.AttachedProperty.Set(true)
            }
        });
    }

    protected override bool OnBackButtonPressed()
    {
        _ = ReloadSettingsAsync();

        return true;
    }

    async Task ReloadSettingsAsync()
    {
        await TryGetDevideInformationAsync();

        (_configuration as IConfigurationRoot).Providers
            .OfType<SettingsConfigurationProvider>()
            .First()
            .Load();

        await Navigation.PopToRootAsync();
    }

    static async Task TryGetDevideInformationAsync()
    {
        var deviceId = "";

        try
        {
            var host = Preferences.Default.Get(nameof(ConnectionSettings.Host), "");
            var username = Preferences.Default.Get(nameof(ConnectionSettings.Username), "");
            var password = Preferences.Default.Get(nameof(ConnectionSettings.Password), "");
            var webPort = Preferences.Default.Get(nameof(ConnectionSettings.WebPort), 80);

            var uriBuilder = new UriBuilder
            {
                Scheme = "http",
                Host = host,
                Port = webPort,
                Path = "cgi-bin/systeminfo_cgi",
            };

            using var client = new HttpClient();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}")));

            var response = await client.GetAsync(uriBuilder.Uri);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var data = content
                    .Split("\n", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Split('=', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                    .ToDictionary(x => x.FirstOrDefault(), x => x.LastOrDefault());

                data.TryGetValue("DeviceUUID", out deviceId);
            }
        }
        catch { };

        Preferences.Default.Set(nameof(ConnectionSettings.DeviceId), deviceId);
    }
}
