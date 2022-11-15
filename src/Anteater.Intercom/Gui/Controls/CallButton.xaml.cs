using System;
using System.Threading;
using System.Threading.Tasks;
using Alanta.Client.Media.Dsp.WebRtc;
using Anteater.Intercom.Services.Audio;
using Anteater.Pipe;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using NAudio.Wave;

namespace Anteater.Intercom.Gui.Controls;

public partial class CallButton : Button
{
    public static readonly DependencyProperty IsCallStartedProperty = DependencyProperty
        .Register(nameof(IsCallStarted), typeof(bool), typeof(CallButton), PropertyMetadata
        .Create(false));

    private readonly ReversAudioService _backChannelConnection;
    private readonly IEventPublisher _pipe;

    private CancellationTokenSource _cts;

    public CallButton()
    {
        _backChannelConnection = App.ServiceProvider.GetRequiredService<ReversAudioService>();
        _pipe = App.ServiceProvider.GetRequiredService<IEventPublisher>();

        var doorLockStateChanged = _pipe.Subscribe<DoorLockStateChanged>(x =>
        {
            DispatcherQueue.TryEnqueue(() => IsEnabled = x.IsLocked);

            return Task.CompletedTask;
        });

        InitializeComponent();

        void UnloadEventHandler()
        {
            _cts?.Cancel();
            doorLockStateChanged.Dispose();
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

        _pipe.Publish(new CallStateChanged(IsCallStarted));

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
        var disconnect = _backChannelConnection.IsOpen == false;

        if (!_backChannelConnection.IsOpen)
        {
            await _backChannelConnection.ConnectAsync();
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
                        await _backChannelConnection.SendAsync(frameBuffer);
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
            DispatcherQueue.TryEnqueue(() => IsCallStarted = false);

            _pipe.Publish(new CallStateChanged(false));

            if (disconnect)
            {
                _backChannelConnection.Disconnect();
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
}
