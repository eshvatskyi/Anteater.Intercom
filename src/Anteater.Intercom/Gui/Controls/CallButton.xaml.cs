using System;
using System.Threading;
using System.Threading.Tasks;
using Alanta.Client.Media.Dsp.WebRtc;
using Anteater.Intercom.Services.ReversChannel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using NAudio.Wave;

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

        IsCallStarted = !IsCallStarted;

        _messenger.Send(new CallStateChanged(IsCallStarted));

        if (IsCallStarted)
        {
            _cts = new CancellationTokenSource();

            Task.Run(StartCallAsync);
        }
        else
        {
            _cts?.Cancel();
        }
    }

    async Task StartCallAsync()
    {
        var disconnect = _reversAudio.IsOpen == false;

        if (!_reversAudio.IsOpen)
        {
            await _reversAudio.ConnectAsync();
        }

        var echoCanceller = new WebRtcFilter(1000, 500, new(), new(), true, false, false);

        var capture = new WasapiLoopbackCapture();
        capture.DataAvailable += (_, args) => echoCanceller.RegisterFramePlayed(args.Buffer);

        var waveIn = new WaveInEvent
        {
            WaveFormat = new WaveFormat(8000, 1),
            BufferMilliseconds = 10
        };

        waveIn.DataAvailable += async (_, args) =>
        {
            echoCanceller.Write(args.Buffer);

            try
            {
                bool moreFrames;
                do
                {
                    var frameBuffer = new short[320];
                    if (echoCanceller.Read(frameBuffer, out moreFrames))
                    {
                        await _reversAudio.SendAsync(frameBuffer);
                    }
                } while (moreFrames);
            }
            catch
            {
                _cts.Cancel();
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

            capture.StopRecording();
            capture.Dispose();

            waveIn.StopRecording();
            waveIn.Dispose();
        });

        capture.StartRecording();
        waveIn.StartRecording();

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
