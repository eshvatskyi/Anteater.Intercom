using System;
using Anteater.Intercom.Services.ReversChannel;
using CommunityToolkit.Mvvm.Messaging;
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

    private readonly IDoorLockService _doorLock;
    private readonly IMessenger _messenger;

    public DoorLockButton()
    {
        _doorLock = App.Services.GetRequiredService<IDoorLockService>();
        _messenger = App.Services.GetRequiredService<IMessenger>();

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

            _messenger.Send(new DoorLockStateChanged(false));

            _doorLock.UnlockDoorAsync().ContinueWith(_ =>
            {
                DispatcherQueue.TryEnqueue(delegate
                {
                    IsDoorLocked = true;
                });

                _messenger.Send(new DoorLockStateChanged(true));
            });
        }
    }
}
