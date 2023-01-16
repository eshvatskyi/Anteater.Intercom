using System;
using System.Threading;
using System.Threading.Tasks;
using Anteater.Intercom.Services.Audio;
using Anteater.Intercom.Services.ReversChannel;
using CommunityToolkit.Mvvm.Messaging;
using FFmpeg.AutoGen.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Anteater.Intercom.Gui.Controls;

public partial class CallButton : Button, IRecipient<DoorLockStateChanged>
{
    public static readonly DependencyProperty IsCallStartedProperty = DependencyProperty
        .Register(nameof(IsCallStarted), typeof(bool), typeof(CallButton), PropertyMetadata
        .Create(false));

    private readonly IMessenger _messenger;
    private readonly IAudioRecord _recorder;
    private readonly IReversAudioService _reversAudio;

    private CancellationTokenSource _cts;

    public CallButton()
    {
        _messenger = App.Services.GetRequiredService<IMessenger>();
        _recorder = App.Services.GetRequiredService<IAudioRecord>();
        _reversAudio = App.Services.GetRequiredService<IReversAudioService>();

        InitializeComponent();

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public bool IsCallStarted
    {
        get => Convert.ToBoolean(GetValue(IsCallStartedProperty));
        set => SetValue(IsCallStartedProperty, value);
    }

    void OnLoaded(object sender, RoutedEventArgs e)
    {
        _messenger.Register(this);
    }

    void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _cts?.Cancel();

        _messenger.UnregisterAll(this);
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
            try
            {
                await _reversAudio.ConnectAsync(_recorder.Format, _recorder.SampleRate, _recorder.Channels);
            }
            catch
            {
                DispatcherQueue.TryEnqueue(delegate
                {
                    IsCallStarted = false;
                });

                return;
            }
        }

        _cts.Token.Register(delegate
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

            _recorder.DataAvailable -= OnRecordingDataAvailable;

            _recorder.Stopped -= OnRecordingStopped;

            _recorder.Stop();
        });


        var tcs = new TaskCompletionSource();

        _cts.Token.Register(delegate
        {
            tcs.TrySetCanceled();
        });

        _recorder.DataAvailable += OnRecordingDataAvailable;

        _recorder.Stopped += OnRecordingStopped;

        try
        {
            _recorder.Start();
        }
        catch
        {
            _cts.Cancel();
        }

        await tcs.Task;
    }

    async void OnRecordingDataAvailable(byte[] data)
    {
        try
        {
            await _reversAudio.SendAsync(data);
        }
        catch
        {
            _cts?.Cancel();
        }
    }

    void OnRecordingStopped()
    {
        _cts.Cancel();
    }

    void IRecipient<DoorLockStateChanged>.Receive(DoorLockStateChanged message)
    {
        DispatcherQueue.TryEnqueue(delegate
        {
            IsEnabled = message.IsLocked;
        });
    }
}
