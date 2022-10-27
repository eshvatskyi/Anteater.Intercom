using System;
using Anteater.Pipe;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Anteater.Intercom.Gui.Controls;

public partial class AlarmMuteButton : Button
{
    public static readonly DependencyProperty IsSoundMutedProperty = DependencyProperty
        .Register(nameof(IsSoundMuted), typeof(bool), typeof(AlarmMuteButton), PropertyMetadata
        .Create(false));

    private readonly IEventPublisher _pipe;

    public AlarmMuteButton()
    {
        _pipe = App.ServiceProvider.GetRequiredService<IEventPublisher>();

        Loaded += OnLoaded;

        InitializeComponent();
    }

    public bool IsSoundMuted
    {
        get => Convert.ToBoolean(GetValue(IsSoundMutedProperty));
        set => SetValue(IsSoundMutedProperty, value);
    }

    void OnLoaded(object sender, RoutedEventArgs e)
    {
        IsSoundMuted = false;

        _pipe.Publish(new AlarmStateChanged(IsSoundMuted));
    }

    protected override void OnTapped(TappedRoutedEventArgs e)
    {
        e.Handled = false;

        IsSoundMuted = !IsSoundMuted;

        _pipe.Publish(new AlarmStateChanged(IsSoundMuted));
    }
}
