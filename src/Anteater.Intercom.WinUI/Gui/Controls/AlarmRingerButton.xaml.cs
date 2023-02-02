using System;
using System.Threading;
using System.Threading.Tasks;
using Anteater.Intercom.Services.Events;
using CommunityToolkit.Extensions.Hosting;
using CommunityToolkit.Mvvm.Messaging;
using CSCore;
using CSCore.Codecs;
using CSCore.SoundOut;
using CSCore.Streams;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Anteater.Intercom.Gui.Controls;

public partial class AlarmRingerButton : Button, IRecipient<AlarmEvent>, IRecipient<AlarmStateChanged>, IRecipient<CallStateChanged>, IRecipient<DoorLockStateChanged>
{
    public static readonly DependencyProperty IsActiveProperty = DependencyProperty
        .Register(nameof(IsActive), typeof(bool), typeof(AlarmRingerButton), PropertyMetadata
        .Create(false, (o, _) => (o as AlarmRingerButton)?.OnIsActiveChanged()));

    private readonly IMessenger _messenger;
    private readonly WaveOut _waveOut;

    private CancellationTokenSource _cts;

    public AlarmRingerButton()
    {
        _messenger = (App.Current as CancelableApplication).Services.GetRequiredService<IMessenger>();

        _waveOut = new WaveOut();

        InitializeComponent();

        Loaded += OnLoaded;
        Unloaded += OnUnload;
    }

    public bool IsActive
    {
        get => Convert.ToBoolean(GetValue(IsActiveProperty));
        set => SetValue(IsActiveProperty, value);
    }

    void OnLoaded(object sender, RoutedEventArgs e)
    {
        _waveOut.Initialize(new LoopStream(CodecFactory.Instance
            .GetCodec("Assets/DoorBell.mp3")
            .ToSampleSource()
            .ToWaveSource()));

        _messenger.RegisterAll(this);

        IsActive = false;
    }

    void OnUnload(object sender, RoutedEventArgs e)
    {
        _messenger.UnregisterAll(this);

        _cts?.Cancel();

        _waveOut.Stop();
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

        _ = Task.Run(_waveOut.Play);
    }

    protected override void OnTapped(TappedRoutedEventArgs e)
    {
        e.Handled = false;

        IsActive = false;
    }

    public int Read(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
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
        try
        {
            _waveOut.Volume = message.IsMuted ? 0 : 1;
        }
        catch { }
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
