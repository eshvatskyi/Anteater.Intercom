using System;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Anteater.Intercom.Gui.Controls;

public partial class SoundMuteButton : Button, IRecipient<CallStateChanged>
{
    public static readonly DependencyProperty IsSoundMutedProperty = DependencyProperty
        .Register(nameof(IsSoundMuted), typeof(bool), typeof(SoundMuteButton), PropertyMetadata
        .Create(false));

    private readonly IMessenger _messenger;

    public SoundMuteButton()
    {
        _messenger = App.Services.GetRequiredService<IMessenger>();

        InitializeComponent();

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public bool IsSoundMuted
    {
        get => Convert.ToBoolean(GetValue(IsSoundMutedProperty));
        set => SetValue(IsSoundMutedProperty, value);
    }

    void OnLoaded(object sender, RoutedEventArgs e)
    {
        _messenger.Register(this);

        IsSoundMuted = true;

        _messenger.Send(new SoundStateChanged(IsSoundMuted));
    }

    void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _messenger.UnregisterAll(this);
    }

    protected override void OnTapped(TappedRoutedEventArgs e)
    {
        e.Handled = false;

        IsSoundMuted = !IsSoundMuted;

        _messenger.Send(new SoundStateChanged(IsSoundMuted));
    }

    void IRecipient<CallStateChanged>.Receive(CallStateChanged message)
    {
        DispatcherQueue.TryEnqueue(delegate
        {
            IsEnabled = !message.IsCalling;

            _messenger.Send(new SoundStateChanged(message.IsCalling ? false : IsSoundMuted));
        });
    }
}
