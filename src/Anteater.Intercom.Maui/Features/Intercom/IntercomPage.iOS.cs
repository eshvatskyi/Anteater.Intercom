using Anteater.Intercom.Core;
using Anteater.Intercom.Features.Settings;
using CommunityToolkit.Maui.Markup;
using Microsoft.Maui.Layouts;

namespace Anteater.Intercom.Features.Intercom;

public partial class IntercomPage
{
    private partial View BuildLayout()
    {
        return new AbsoluteLayout
        {
            new Image()
                .Bind(Image.SourceProperty, Markup.FullName(static (IntercomViewModel x) => x.Player.ImageSource))
                .Bind(Image.WidthProperty, Markup.FullName(static (IntercomViewModel x) => x.Player.ImageWidth), BindingMode.OneWayToSource)
                .Bind(Image.HeightProperty, Markup.FullName(static (IntercomViewModel x) => x.Player.ImageHeight), BindingMode.OneWayToSource)
                .ZIndex(-1)
                .LayoutFlags(AbsoluteLayoutFlags.All)
                .LayoutBounds(0, 0, 1, 1),

            new VerticalStackLayout
            {
                new ImageButton()
                    .Size(65)
                    .Source(ImageSource.FromFile("settings.png"))
                    .Bind(ImageButton.IsEnabledProperty, static (IntercomViewModel x) => x.IsSettingsEnaled)
                    .TapGesture(async () => await Navigation.PushAsync(_services.GetRequiredService<SettingsPage>())),
            }
            .Spacing(20)
            .Margins(0, 20, 20, 20)
            .LayoutFlags(AbsoluteLayoutFlags.PositionProportional | AbsoluteLayoutFlags.HeightProportional)
            .LayoutBounds(1, 0.5, 95, 1),

            new HorizontalStackLayout
            {
                new ImageButton
                {
                    Triggers =
                    {
                        new DataTrigger(typeof(ImageButton))
                        {
                            Binding = new Binding(Markup.FullName(static (IntercomViewModel x) => x.Door.IsLocked)),
                            Value = true,
                            Setters =
                            {
                                new Setter { Property = ImageButton.SourceProperty, Value = ImageSource.FromFile("doorlock.png"), },
                            },
                        },
                        new DataTrigger(typeof(ImageButton))
                        {
                            Binding = new Binding(Markup.FullName(static (IntercomViewModel x) => x.Door.IsLocked)),
                            Value = false,
                            Setters =
                            {
                                new Setter { Property = ImageButton.SourceProperty, Value = ImageSource.FromFile("doorunlock.png"), },
                            },
                        },
                    },
                }
                .Size(100)
                .Margins(0, 0, 20, 0)
                .Bind(ImageButton.IsEnabledProperty, Markup.FullName(static (IntercomViewModel x) => x.Door.IsLocked))
                .BindCommand(Markup.FullName(static (IntercomViewModel x) => x.Door.Unlock)),

                new ImageButton
                {
                    Triggers =
                    {
                        new DataTrigger(typeof(ImageButton))
                        {
                            Binding = new Binding(Markup.FullName(static (IntercomViewModel x) => x.Call.IsStarted)),
                            Value = true,
                            Setters =
                            {
                                new Setter { Property = ImageButton.SourceProperty, Value = ImageSource.FromFile("callend.png"), },
                            },
                        },
                        new DataTrigger(typeof(ImageButton))
                        {
                            Binding = new Binding(Markup.FullName(static (IntercomViewModel x) => x.Call.IsStarted)),
                            Value = false,
                            Setters =
                            {
                                new Setter { Property = ImageButton.SourceProperty, Value = ImageSource.FromFile("callstart.png"), },
                            },
                        },
                    },
                }
                .Size(100)
                .Bind(ImageButton.IsEnabledProperty, Markup.FullName(static(IntercomViewModel x) => x.Door.IsLocked))
                .BindCommand(Markup.FullName(static(IntercomViewModel x) => x.Call.Start)),
            }
            .Margins(0, 0, 0, 20)
            .LayoutFlags(AbsoluteLayoutFlags.PositionProportional)
            .LayoutBounds(0.5, 1, -1, 120),
        };
    }
}
