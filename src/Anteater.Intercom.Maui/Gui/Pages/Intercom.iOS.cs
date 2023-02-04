using System.Windows.Input;
using Anteater.Intercom.Gui.ViewModels;
using Microsoft.Maui.Layouts;

namespace Anteater.Intercom.Gui.Pages;

using Sharp.UI;

public partial class Intercom
{
    private partial View BuildLayout()
    {
        return new AbsoluteLayout()
        {
            new Image(x => x
                .ZIndex(-1)
                .AbsoluteLayoutBounds(new Rect(0, 0, 1, 1))
                .AbsoluteLayoutFlags(AbsoluteLayoutFlags.All)
                .Source(x => x.Path<ImageSource, IntercomViewModel>(x => x.Player.ImageSource))
                .Width(x => x.Path<VisualElement, IntercomViewModel>(x => x.Player.ImageWidth).BindingMode(BindingMode.OneWayToSource))
                .Height(x => x.Path<VisualElement, IntercomViewModel>(x => x.Player.ImageHeight).BindingMode(BindingMode.OneWayToSource))
            ),

            new VerticalStackLayout(x => x
                .AbsoluteLayoutBounds(new Rect(1, 0.5, 95, 1))
                .AbsoluteLayoutFlags(AbsoluteLayoutFlags.PositionProportional | AbsoluteLayoutFlags.HeightProportional)
                .Margin(new Thickness(0, 20, 20, 20))
                .Spacing(20)
            )
            {
                new ImageButton(x => x
                    .WidthRequest(75)
                    .HeightRequest(75)
                    .Source(ImageSource.FromFile("settings.png"))
                    .IsEnabled(x => x.Path<bool, IntercomViewModel>(x => x.IsSettingsEnaled))
                    .OnClicked(async (_) => await Navigation.PushAsync(_services.GetRequiredService<Settings>()))
                ),
            },

            new HorizontalStackLayout(x => x
                .AbsoluteLayoutBounds(new Rect(0.5, 1, -1, 120))
                .AbsoluteLayoutFlags(AbsoluteLayoutFlags.PositionProportional)
                .Margin(new Thickness(0, 0, 0, 20))
            )
            {
                new ImageButton(x => x
                    .WidthRequest(100)
                    .HeightRequest(100)
                    .Margin(new Thickness(0, 0, 20, 0))
                    .Source(ImageSource.FromFile("doorlock.png"))
                    .IsEnabled(x => x.Path<bool, IntercomViewModel>(x => x.Door.IsLocked))
                    .Command(x => x.Path<ICommand, IntercomViewModel>(x => x.Door.Unlock))
                    .Triggers(
                        new DataTrigger<ImageButton>(x => x.Path<IntercomViewModel>(x => x.Door.IsLocked), true)
                        {
                            ImageButton.SourceProperty.Set(ImageSource.FromFile("doorlock.png")),
                        },
                        new DataTrigger<ImageButton>(x => x.Path<IntercomViewModel>(x => x.Door.IsLocked), false)
                        {
                            ImageButton.SourceProperty.Set(ImageSource.FromFile("doorunlock.png")),
                        }
                    )
                ),
                new ImageButton(x => x
                    .WidthRequest(100)
                    .HeightRequest(100)
                    .Source(ImageSource.FromFile("callstart.png"))
                    .IsEnabled(x => x.Path<bool, IntercomViewModel>(x => x.Door.IsLocked))
                    .Command(x => x.Path<ICommand, IntercomViewModel>(x => x.Call.Start))
                    .Triggers(
                        new DataTrigger<ImageButton>(x => x.Path<IntercomViewModel>(x => x.Call.IsStarted), true)
                        {
                            ImageButton.SourceProperty.Set(ImageSource.FromFile("callend.png")),
                        },
                        new DataTrigger<ImageButton>(x => x.Path<IntercomViewModel>(x => x.Call.IsStarted), false)
                        {
                            ImageButton.SourceProperty.Set(ImageSource.FromFile("callstart.png")),
                        }
                    )
                ),
            },
        };
    }
}
