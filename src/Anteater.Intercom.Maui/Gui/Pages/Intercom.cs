using Anteater.Intercom.Gui.Messages;
using Anteater.Intercom.Gui.ViewModels;
using CommunityToolkit.Mvvm.Messaging;

namespace Anteater.Intercom.Gui.Pages;

using Sharp.UI;

public partial class Intercom : ContentPage
{
    private readonly IServiceProvider _services;
    private readonly IMessenger _messenger;

    public Intercom(IServiceProvider services, IMessenger messenger, IntercomViewModel viewModel)
    {
        _services = services;
        _messenger = messenger;

        NavigationPage.SetHasNavigationBar(this, false);

        BindingContext = viewModel;

        Build();

        Loaded += (_, _) =>
        {
            viewModel.Player.Connect();

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
        };
    }

    void Build()
    {
        Content = BuildLayout();

        Resources = new ResourceDictionary
        {
            new Style<ImageButton>(applyToDerivedTypes: true)
            {
                ImageButton.OpacityProperty.Set(1),
                ImageButton.BorderColorProperty.Set(Colors.Transparent),
                ImageButton.BorderWidthProperty.Set(0),
                ImageButton.CornerRadiusProperty.Set(30),
                ImageButton.PaddingProperty.Set(10),

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
    }

    private partial View BuildLayout();
}
