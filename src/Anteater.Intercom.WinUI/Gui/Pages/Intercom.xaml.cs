using System;
using System.Threading;
using System.Threading.Tasks;
using Anteater.Intercom.Services.Events;
using CommunityToolkit.Extensions.Hosting;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Anteater.Intercom.Gui.Pages;

sealed partial class Intercom : Page, IRecipient<AlarmEvent>, IRecipient<CallStateChanged>
{
    public static readonly DependencyProperty IsOverlayVisibleProperty = DependencyProperty
        .Register(nameof(IsOverlayVisible), typeof(bool), typeof(Intercom), PropertyMetadata
        .Create(false));

    private readonly IMessenger _messenger;

    private Task _commandTask = Task.CompletedTask;
    private CancellationTokenSource _cts;
    private bool _overlayLocked = false;

    public Intercom()
    {
        _messenger = (App.Current as CancelableApplication).Services.GetRequiredService<IMessenger>();

        InitializeComponent();

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public bool IsOverlayVisible
    {
        get => Convert.ToBoolean(GetValue(IsOverlayVisibleProperty));
        set => SetValue(IsOverlayVisibleProperty, value);
    }

    void OnLoaded(object sender, RoutedEventArgs e)
    {
        _messenger.RegisterAll(this);

        IsOverlayVisible = true;

        ApplyOverlayChanges(skipMessage: true);

        _ = Task.Run(_videoPlayer.Connect);
    }

    void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _messenger.UnregisterAll(this);

        _cts?.Cancel();
    }

    protected override void OnTapped(TappedRoutedEventArgs e)
    {
        e.Handled = true;
        IsOverlayVisible = true;
        ApplyOverlayChanges();
    }

    protected override void OnDoubleTapped(DoubleTappedRoutedEventArgs e)
    {
        e.Handled = true;
        MainWindow.Instance.FullScreenMode = !MainWindow.Instance.FullScreenMode;
    }

    void ApplyOverlayChanges(bool skipMessage = false)
    {
        if (_overlayLocked)
        {
            return;
        }

        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        if (IsOverlayVisible)
        {
            if (!skipMessage)
            {
                _commandTask = _commandTask.ContinueWith(delegate
                {
                    _messenger.Send(new ChangeVideoState(false));
                });
            }

            _ = Task.Delay(TimeSpan.FromSeconds(15), _cts.Token).ContinueWith(delegate
            {
                _cts = null;

                DispatcherQueue.TryEnqueue(delegate
                {
                    IsOverlayVisible = false;
                    ApplyOverlayChanges();
                });
            }, TaskContinuationOptions.OnlyOnRanToCompletion);
        }
        else
        {
            _ = Task.Delay(TimeSpan.FromSeconds(30), _cts.Token).ContinueWith(delegate
            {
                _cts = null;

                _commandTask = _commandTask.ContinueWith(delegate
                {
                    _messenger.Send(new ChangeVideoState(true));
                });
            }, TaskContinuationOptions.OnlyOnRanToCompletion);
        }
    }

    void Button_Tapped(object sender, TappedRoutedEventArgs e)
    {
        MainWindow.Instance.NavigateToType(typeof(Settings));
    }

    void IRecipient<AlarmEvent>.Receive(AlarmEvent message)
    {
        if (message.Status && message.Type switch
        {
            AlarmEvent.EventType.MotionDetection => true,
            AlarmEvent.EventType.SensorAlarm => true,
            _ => false
        })
        {
            DispatcherQueue.TryEnqueue(delegate
            {
                IsOverlayVisible = true;
                ApplyOverlayChanges();
            });
        }
    }

    void IRecipient<CallStateChanged>.Receive(CallStateChanged message)
    {
        if (_overlayLocked = message.IsCalling)
        {
            _cts?.Cancel();
        }
        else
        {
            DispatcherQueue.TryEnqueue(delegate
            {
                IsOverlayVisible = true;
                ApplyOverlayChanges();
            });
        }
    }
}
