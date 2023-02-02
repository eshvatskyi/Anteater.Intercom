using Anteater.Intercom.Gui.ViewModels;
using Microsoft.Maui.Layouts;

namespace Anteater.Intercom.Gui.Pages;

using Sharp.UI;

public class Intercom : ContentPage
{
    private readonly Settings _settingsPage;

    public Intercom(IntercomViewModel viewModel, Settings settingsPage)
    {
        _settingsPage = settingsPage;

        NavigationPage.SetHasNavigationBar(this, false);

        Loaded += (_, _) => BuildControls(viewModel);
    }

    void BuildControls(IntercomViewModel viewModel)
    {
        BindingContext = viewModel;

        Content = new AbsoluteLayout()
        {
            new Image(x => x
                .ZIndex(-1)
                .AbsoluteLayoutBounds(new Rect(0, 0, 1, 1))
                .AbsoluteLayoutFlags(AbsoluteLayoutFlags.PositionProportional | AbsoluteLayoutFlags.SizeProportional)
                .Source(x => x.Path(nameof(viewModel.ImageSource)))
                .OnSizeChanged(x =>
                {
                    viewModel.ImageWidth = (int)x.Width;
                    viewModel.ImageHeight = (int)x.Height;
                })
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
                    .IsEnabled(x => x.Path(nameof(viewModel.IsDoorLocked)))
                    .OnClicked((_) => Navigation.PushAsync(_settingsPage))
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
                    .IsEnabled(x => x.Path(nameof(viewModel.IsDoorLocked)))
                    .Command(x => x.Path(nameof(viewModel.UnlockDoor)))
                    .Triggers(
                        new DataTrigger<ImageButton>(x => x.Path(nameof(viewModel.IsDoorLocked)), true)
                        {
                            ImageButton.SourceProperty.Set(ImageSource.FromFile("doorlock.png")),
                        },
                        new DataTrigger<ImageButton>(x => x.Path(nameof(viewModel.IsDoorLocked)), false)
                        {
                            ImageButton.SourceProperty.Set(ImageSource.FromFile("doorunlock.png")),
                        }
                    )
                ),
                new ImageButton(x => x
                    .WidthRequest(100)
                    .HeightRequest(100)
                    .Source(ImageSource.FromFile("callstart.png"))
                    .IsEnabled(x => x.Path(nameof(viewModel.IsDoorLocked)))
                    .Command(x => x.Path(nameof(viewModel.StartCall)))
                    .Triggers(
                        new DataTrigger<ImageButton>(x => x.Path(nameof(viewModel.IsCallStarted)), true)
                        {
                            ImageButton.SourceProperty.Set(ImageSource.FromFile("callend.png")),
                        },
                        new DataTrigger<ImageButton>(x => x.Path(nameof(viewModel.IsCallStarted)), false)
                        {
                            ImageButton.SourceProperty.Set(ImageSource.FromFile("callstart.png")),
                        }
                    )
                ),
            },
        };

        Content.Resources = new ResourceDictionary
        {
            new Style<ImageButton>(applyToDerivedTypes: true)
            {
                ImageButton.OpacityProperty.Set(1),
                ImageButton.BorderColorProperty.Set(Colors.Transparent),
                ImageButton.BorderWidthProperty.Set(0),
                ImageButton.CornerRadiusProperty.Set(30),
                ImageButton.PaddingProperty.Set(15),

                new VisualState(VisualState.ImageButton.Normal)
                {
                    VisualElement.BackgroundColorProperty.Set(Colors.Black.WithAlpha(0.2f)),
                },
                new VisualState(VisualState.ImageButton.Focused)
                {
                    VisualElement.BackgroundColorProperty.Set(Colors.Black.WithAlpha(0.2f)),
                },
                new VisualState(VisualState.ImageButton.Pressed)
                {
                    VisualElement.BackgroundColorProperty.Set(Colors.Green.WithAlpha(0.2f)),
                },
                new VisualState(VisualState.ImageButton.Disabled)
                {
                    VisualElement.OpacityProperty.Set(.2),
                },
            },
        };

        Appearing += (_, _) => _ = Task.Run(viewModel.Connect);

        Disappearing += (_, _) => _ = Task.Run(viewModel.Stop);

        _ = Task.Run(viewModel.Connect);
    }
}

