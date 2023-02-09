using Anteater.Intercom.Core;
using Anteater.Intercom.Messages;
using CommunityToolkit.Maui.Markup;
using CommunityToolkit.Mvvm.Messaging;

namespace Anteater.Intercom.Features.Intercom;

public partial class IntercomPage : ContentPageBase
{
    private readonly IServiceProvider _services;
    private readonly IMessenger _messenger;

    public IntercomPage(IServiceProvider services, IMessenger messenger, IntercomViewModel viewModel)
    {
        _services = services;
        _messenger = messenger;

        NavigationPage.SetHasNavigationBar(this, false);

        BindingContext = viewModel;

        Loaded += (_, _) =>
        {
            Build();

            Appearing += (_, _) =>
            {
                _messenger.Register<WindowStateChanged>(this, (_, message) =>
                {
                    switch (message.State)
                    {
                        case WindowState.Resumed:
                            if (!viewModel.Player.IsConnected)
                            {
                                viewModel.Player.Connect();
                            }
                            break;

                        case WindowState.Stopped:
                            viewModel.Player.Stop();
                            break;

                        case WindowState.Closing:
                            viewModel.Player.Stop();
                            break;
                    }
                });

                viewModel.ShowOverlay.Execute(true);
                viewModel.Player.Connect();
            };

            Disappearing += (_, _) =>
            {
                _messenger.UnregisterAll(this);

                viewModel.Player.Stop();
            };

            viewModel.ShowOverlay.Execute(true);
            viewModel.Player.Connect();
        };
    }

    protected override void Build()
    {
        Content = BuildLayout();

        Resources = new ResourceDictionary
        {
            new Style<ImageButton>(
                (ImageButton.OpacityProperty, 1),
                (ImageButton.BorderColorProperty, Colors.Transparent),
                (ImageButton.BorderWidthProperty, 0),
                (ImageButton.CornerRadiusProperty, 30),
                (ImageButton.PaddingProperty, 10),
                (VisualStateManager.VisualStateGroupsProperty, new VisualStateGroupList
                {
                    new VisualStateGroup
                    {
                        States =
                        {
                            new VisualState
                            {
                                Name = "Normal",
                                Setters =
                                {
                                    new Setter { Property = VisualElement.BackgroundColorProperty, Value = Colors.Black.WithAlpha(0.2f) },
                                },
                            },
                            new VisualState
                            {
                                Name = "Focused",
                                Setters =
                                {
                                    new Setter { Property = VisualElement.BackgroundColorProperty, Value = Colors.Black.WithAlpha(0.2f) },
                                },
                            },
                            new VisualState
                            {
                                Name = "Pressed",
                                Setters =
                                {
                                    new Setter { Property = VisualElement.BackgroundColorProperty, Value = Colors.Green.WithAlpha(0.2f) },
                                },
                            },
                            new VisualState
                            {
                                Name = "Disabled",
                                Setters =
                                {
                                    new Setter { Property = VisualElement.OpacityProperty, Value = 0.2 },
                                },
                            },
                        },
                    },
                })
            ).ApplyToDerivedTypes(true),
        };
    }

    private partial View BuildLayout();
}
