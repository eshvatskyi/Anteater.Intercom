using System.Net.Http.Headers;
using System.Text;
using Anteater.Intercom.Core;
using Anteater.Intercom.Services.Settings;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Markup;
using Microsoft.Maui.Layouts;

namespace Anteater.Intercom.Features.Settings;

public class SettingsPage : ContentPageBase
{
    private readonly ISettingsService _settings;
    private readonly ISettingsProvider _settingsProvider;

    public SettingsPage(ISettingsService settings, ISettingsProvider settingsProvider)
    {
        _settings = settings;
        _settingsProvider = settingsProvider;

        NavigationPage.SetHasNavigationBar(this, false);

        Build();
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
                    .Key("WebPort"),

                new SettingEntry { Keyboard = Keyboard.Numeric, ReturnType = ReturnType.Next, }
                    .Placeholder("RtspPort")
                    .Key("RtspPort"),

                new SettingEntry { Keyboard = Keyboard.Numeric, ReturnType = ReturnType.Next, }
                    .Placeholder("DataPort")
                    .Key("DataPort"),
            }
            .Spacing(10)
            .Padding(20, 10)
            .LayoutFlags(AbsoluteLayoutFlags.XProportional | AbsoluteLayoutFlags.SizeProportional)
            .LayoutBounds(0, 40, 1, 1)
            .Assign(out Layout container),
        };

        var settingEntries = new LinkedList<SettingEntry>(container.Children.OfType<SettingEntry>());

        for (var node = settingEntries.First; node != null; node = node.Next)
        {
            SetFocusOnEntryCompletedBehavior.SetNextElement(node.Value, node.Next?.Value ?? settingEntries.First.Value);
        }

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

        _settings.Refresh();

        await Navigation.PopToRootAsync();
    }

    async ValueTask TryGetDevideInformationAsync()
    {
        var settings = _settingsProvider.Get();
        if (settings == _settings.Current)
        {
            return;
        }

        var deviceId = "";

        try
        {

            var uriBuilder = new UriBuilder
            {
                Scheme = "http",
                Host = settings.Host,
                Port = settings.WebPort,
                Path = "cgi-bin/systeminfo_cgi",
            };

            using var client = new HttpClient();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{settings.Username}:{settings.Password}")));

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

        _settingsProvider.Set(nameof(ConnectionSettings.DeviceId), deviceId);
    }
}
