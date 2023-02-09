using System.Net.Http.Headers;
using System.Text;
using Anteater.Intercom.Core;
using Anteater.Intercom.Services.Settings;
using Anteater.Intercom.Settings;
using CommunityToolkit.Maui.Markup;
using Microsoft.Extensions.Configuration;
using Microsoft.Maui.Layouts;

namespace Anteater.Intercom.Features.Settings;

public class SettingsPage : ContentPageBase
{
    private readonly IConfiguration _configuration;

    public SettingsPage(IConfiguration configuration)
    {
        _configuration = configuration;

        NavigationPage.SetHasNavigationBar(this, false);
    }

    protected override void Build()
    {
        Content = new AbsoluteLayout
        {
            new AbsoluteLayout
            {
                new Button()
                    .Text("Back")
                    .TextColor(Colors.White)
                    .TapGesture(() => SendBackButtonPressed())
                    .LayoutFlags(AbsoluteLayoutFlags.PositionProportional)
                    .LayoutBounds(0, 0.5, -1, -1),

                new Label()
                    .Text("Settings")
                    .TextColor(Colors.White)
                    .Font(size: 18, bold: true)
                    .LayoutFlags(AbsoluteLayoutFlags.PositionProportional)
                    .LayoutBounds(0.5, 0.5, -1, -1),
            }
            .Padding(20, 10)
            .LayoutFlags(AbsoluteLayoutFlags.PositionProportional | AbsoluteLayoutFlags.WidthProportional)
            .LayoutBounds(0, 0, 1, 40),

            new VerticalStackLayout
            {
                new SettingEntry { ReturnType = ReturnType.Next, }
                    .Placeholder("Host")
                    .Key("Host"),

                new SettingEntry { ReturnType = ReturnType.Next, }
                    .Placeholder("Username")
                    .Key("Username"),

                new SettingEntry { IsPassword = true, ReturnType = ReturnType.Next, }
                    .Placeholder("Password")
                    .Key("Password"),

                new SettingEntry { Keyboard = Keyboard.Numeric, ReturnType = ReturnType.Next, }
                    .Placeholder("WebPort")
                    .Key("WebPort")
                    .Default("80"),

                new SettingEntry { Keyboard = Keyboard.Numeric, ReturnType = ReturnType.Next, }
                    .Placeholder("RtspPort")
                    .Key("RtspPort")
                    .Default("554"),

                new SettingEntry { Keyboard = Keyboard.Numeric, ReturnType = ReturnType.Next, }
                    .Placeholder("DataPort")
                    .Key("DataPort")
                    .Default("5000"),
            }
            .Spacing(10)
            .Padding(20, 10)
            .LayoutFlags(AbsoluteLayoutFlags.XProportional | AbsoluteLayoutFlags.SizeProportional)
            .LayoutBounds(0, 40, 1, 1),
        };

        Resources = new ResourceDictionary
        {
            new Style<SettingEntry>(SettingBehavior.AttachBehaviorProperty, true),
        };
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
