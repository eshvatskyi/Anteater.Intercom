using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Anteater.Intercom.Services.Events;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using NAudio.Wave;

namespace Anteater.Intercom.Gui.Controls;

public partial class AlarmRingerButton : Button,
    IRecipient<AlarmEvent>,
    IRecipient<AlarmStateChanged>,
    IRecipient<CallStateChanged>,
    IRecipient<DoorLockStateChanged>
{
    public static readonly DependencyProperty IsActiveProperty = DependencyProperty
        .Register(nameof(IsActive), typeof(bool), typeof(AlarmRingerButton), PropertyMetadata
        .Create(false, (o, _) => (o as AlarmRingerButton)?.OnIsActiveChanged()));

    private readonly IMessenger _messenger;
    private readonly WaveOut _waveOut;

    private CancellationTokenSource _cts;

    public AlarmRingerButton()
    {
        _messenger = App.Services.GetRequiredService<IMessenger>();

        _messenger.RegisterAll(this);

        _waveOut = new WaveOut();

        Loaded += OnLoaded;

        InitializeComponent();

        void UnloadEventHandler()
        {
            _cts?.Cancel();
            _waveOut.Stop();
            _waveOut.Dispose();
            _messenger.UnregisterAll(this);
        };

        Unloaded += (_, _) => UnloadEventHandler();
        MainWindow.Instance.Closed += (_, _) => UnloadEventHandler();
    }

    public bool IsActive
    {
        get => Convert.ToBoolean(GetValue(IsActiveProperty));
        set => SetValue(IsActiveProperty, value);
    }

    void OnIsActiveChanged()
    {
        _cts?.Cancel();

        if (!IsActive)
        {
            _waveOut.Stop();

            return;
        }

        _cts = new CancellationTokenSource();

        _ = Task.Delay(TimeSpan.FromSeconds(15), _cts.Token).ContinueWith(_ =>
        {
            DispatcherQueue.TryEnqueue(delegate { IsActive = false; });
        });

        _waveOut.Play();
    }

    void OnLoaded(object sender, RoutedEventArgs e)
    {
        IsActive = false;

        var stream = File.OpenRead("Assets/DoorBell.mp3");

        _waveOut.Init(new WaveLoopStream(new Mp3FileReader(stream)));
    }

    protected override void OnTapped(TappedRoutedEventArgs e)
    {
        e.Handled = false;

        IsActive = false;
    }

    void IRecipient<AlarmEvent>.Receive(AlarmEvent message)
    {
        if (message.Status && message.Type == AlarmEvent.EventType.SensorAlarm)
        {
            DispatcherQueue.TryEnqueue(delegate
            {
                IsActive = true;
            });
        }
    }

    void IRecipient<AlarmStateChanged>.Receive(AlarmStateChanged message)
    {
        _waveOut.Volume = message.IsMuted ? 0 : 1;
    }

    void IRecipient<CallStateChanged>.Receive(CallStateChanged message)
    {
        DispatcherQueue.TryEnqueue(delegate
        {
            IsActive = false;
        });
    }

    void IRecipient<DoorLockStateChanged>.Receive(DoorLockStateChanged message)
    {
        DispatcherQueue.TryEnqueue(delegate
        {
            IsActive = false;
        });
    }
}
