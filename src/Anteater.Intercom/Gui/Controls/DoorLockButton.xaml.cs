using System;
using Anteater.Intercom.Services.Audio;
using Anteater.Pipe;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Anteater.Intercom.Gui.Controls;

public partial class DoorLockButton : Button
{
    public static readonly DependencyProperty IsDoorLockedProperty = DependencyProperty
        .Register(nameof(IsDoorLocked), typeof(bool), typeof(DoorLockButton), PropertyMetadata
        .Create(false));

    private readonly ReversAudioService _backChannelConnection;
    private readonly IEventPublisher _pipe;

    public DoorLockButton()
    {
        _backChannelConnection = App.ServiceProvider.GetRequiredService<ReversAudioService>();
        _pipe = App.ServiceProvider.GetRequiredService<IEventPublisher>();

        Loaded += OnLoaded;

        InitializeComponent();        
    }

    void OnLoaded(object sender, RoutedEventArgs e)
    {
        IsDoorLocked = true;
    }

    public bool IsDoorLocked
    {
        get => Convert.ToBoolean(GetValue(IsDoorLockedProperty));
        set => SetValue(IsDoorLockedProperty, value);
    }

    protected override void OnTapped(TappedRoutedEventArgs e)
    {
        e.Handled = false;

        if (IsDoorLocked)
        {
            IsDoorLocked = false;

            _pipe.Publish(new DoorLockStateChanged(false));

            _backChannelConnection.UnlockDoorAsync().ContinueWith(_ =>
            {
                DispatcherQueue.TryEnqueue(() => IsDoorLocked = true);

                _pipe.Publish(new DoorLockStateChanged(true));
            });
        }
    }
}
