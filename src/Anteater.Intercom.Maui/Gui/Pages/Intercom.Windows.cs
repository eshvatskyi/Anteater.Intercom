using System.Windows.Input;
using Anteater.Intercom.Gui.ViewModels;
using Microsoft.Maui.Layouts;
using Microsoft.Maui.LifecycleEvents;

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
                .IsVisible(x => x.Path<bool, IntercomViewModel>(x => x.IsOverlayVisible))
                .Margin(new Thickness(0, 20, 20, 20))
                .Spacing(20)
            )
            {
                new ImageButton(x => x
                    .WidthRequest(65)
                    .HeightRequest(65)
                    .Source(ImageSource.FromFile("settings.png"))
                    .IsEnabled(x => x.Path<bool, IntercomViewModel>(x => x.IsSettingsEnaled))
                    .OnClicked(async _ => await Navigation.PushAsync(_services.GetRequiredService<Settings>()))
                ),
                new ImageButton(x => x
                    .WidthRequest(65)
                    .HeightRequest(65)
                    .Source(ImageSource.FromFile("soundoff.png"))
                    .IsEnabled(x => x.Path<bool, IntercomViewModel>(x => x.IsSettingsEnaled))
                    .Command(x => x.Path<ICommand, IntercomViewModel>(x => x.Settings.SwitchSoundState))
                    .Triggers(
                        new DataTrigger<ImageButton>(x => x.Path<IntercomViewModel>(x => x.Settings.IsSoundMuted), true)
                        {
                            ImageButton.SourceProperty.Set(ImageSource.FromFile("soundoff.png")),
                        },
                        new DataTrigger<ImageButton>(x => x.Path<IntercomViewModel>(x => x.Settings.IsSoundMuted), false)
                        {
                            ImageButton.SourceProperty.Set(ImageSource.FromFile("soundon.png")),
                        }
                    )
                ),
                new ImageButton(x => x
                    .WidthRequest(65)
                    .HeightRequest(65)
                    .Source(ImageSource.FromFile("bellringon.png"))
                    .IsEnabled(x => x.Path<bool, IntercomViewModel>(x => x.IsSettingsEnaled))
                    .Command(x => x.Path<ICommand, IntercomViewModel>(x => x.Settings.SwitchAlarmState))
                    .Triggers(
                        new DataTrigger<ImageButton>(x => x.Path<IntercomViewModel>(x => x.Settings.IsAlarmMuted), true)
                        {
                            ImageButton.SourceProperty.Set(ImageSource.FromFile("bellringoff.png")),
                        },
                        new DataTrigger<ImageButton>(x => x.Path<IntercomViewModel>(x => x.Settings.IsAlarmMuted), false)
                        {
                            ImageButton.SourceProperty.Set(ImageSource.FromFile("bellringon.png")),
                        }
                    )
                ),
            },

            new HorizontalStackLayout(x => x
                .AbsoluteLayoutBounds(new Rect(0.5, 1, -1, 120))
                .AbsoluteLayoutFlags(AbsoluteLayoutFlags.PositionProportional)
                .IsVisible(x => x.Path<bool, IntercomViewModel>(x => x.IsOverlayVisible))
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
            new ImageButton(x => x
                .AbsoluteLayoutBounds(new Rect(1, 1, -1, 120))
                .AbsoluteLayoutFlags(AbsoluteLayoutFlags.PositionProportional)
                .Margin(new Thickness(0, 0, 20, 20))
                .WidthRequest(100)
                .HeightRequest(100)
                .Source(ImageSource.FromFile("bellringon.png"))
                .IsVisible(x => x.Path<bool, IntercomViewModel>(x => x.Settings.IsAlarmActive))
                .Command(x => x.Path<ICommand, IntercomViewModel>(x => x.Settings.MuteAlarm))
            ),
        }.GestureRecognizers(
            new TapGestureRecognizer(x => x
                .NumberOfTapsRequired(1)
                .Command(x => x.Path<ICommand, IntercomViewModel>(x => x.ShowOverlay))
            ).MauiObject,
            new TapGestureRecognizer(x => x
                .NumberOfTapsRequired(2)
                .OnTapped((_) => _services.GetRequiredService<ILifecycleEventService>().InvokeEvents("WindowFullScreenSwitchRequested"))
            ).MauiObject
        );
    }
}
