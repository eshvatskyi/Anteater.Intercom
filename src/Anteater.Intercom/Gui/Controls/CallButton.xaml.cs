using System;
using System.Threading;
using System.Threading.Tasks;
using Anteater.Intercom.Services.ReversChannel;
using CommunityToolkit.Mvvm.Messaging;
using CSCore;
using CSCore.SoundIn;
using FFmpeg.AutoGen.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Anteater.Intercom.Gui.Controls;

public partial class CallButton : Button,
    IRecipient<DoorLockStateChanged>
{
    public static readonly DependencyProperty IsCallStartedProperty = DependencyProperty
        .Register(nameof(IsCallStarted), typeof(bool), typeof(CallButton), PropertyMetadata
        .Create(false));

    private readonly IReversAudioService _reversAudio;
    private readonly IMessenger _messenger;

    private CancellationTokenSource _cts;

    public CallButton()
    {
        _reversAudio = App.Services.GetRequiredService<IReversAudioService>();
        _messenger = App.Services.GetRequiredService<IMessenger>();

        _messenger.Register(this);

        InitializeComponent();

        void UnloadEventHandler()
        {
            _cts?.Cancel();
            _messenger.UnregisterAll(this);
        };

        Unloaded += (_, _) => UnloadEventHandler();
        MainWindow.Instance.Closed += (_, _) => UnloadEventHandler();
    }

    public bool IsCallStarted
    {
        get => Convert.ToBoolean(GetValue(IsCallStartedProperty));
        set => SetValue(IsCallStartedProperty, value);
    }

    protected override void OnTapped(TappedRoutedEventArgs e)
    {
        e.Handled = false;

        var isCallStarted = IsCallStarted = !IsCallStarted;

        _ = Task.Run(async () =>
        {
            _messenger.Send(new CallStateChanged(isCallStarted));

            if (isCallStarted)
            {
                _cts = new CancellationTokenSource();

                try
                {
                    await StartCallAsync();
                }
                catch { }
            }
            else
            {
                _cts?.Cancel();
            }
        });
    }

    async Task StartCallAsync()
    {
        var disconnect = _reversAudio.IsOpen == false;

        if (!_reversAudio.IsOpen)
        {
            await _reversAudio.ConnectAsync(AVSampleFormat.AV_SAMPLE_FMT_S16, 44100, 1);
        }

        var waveIn = new WaveIn(new WaveFormat(44100, 16, 1));

        waveIn.Initialize();

        waveIn.DataAvailable += (_, args) =>
        {
            try
            {
                _reversAudio.SendAsync(args.Data);
            }
            catch
            {
                _cts?.Cancel();
            }
        };

        _cts.Token.Register(() =>
        {
            DispatcherQueue.TryEnqueue(delegate
            {
                IsCallStarted = false;
            });

            _messenger.Send(new CallStateChanged(false));

            if (disconnect)
            {
                _reversAudio.Disconnect();
            }

            waveIn.Stop();
            waveIn.Dispose();
        });

        waveIn.Start();

        var tcs = new TaskCompletionSource();

        _cts.Token.Register(() => tcs.TrySetCanceled());

        await tcs.Task;
    }

    void IRecipient<DoorLockStateChanged>.Receive(DoorLockStateChanged message)
    {
        DispatcherQueue.TryEnqueue(delegate
        {
            IsEnabled = message.IsLocked;
        });
    }
}
