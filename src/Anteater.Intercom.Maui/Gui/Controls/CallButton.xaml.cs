using Anteater.Intercom.Gui.Messages;
using Anteater.Intercom.Services.Audio;
using Anteater.Intercom.Services.ReversChannel;
using CommunityToolkit.Mvvm.Messaging;

namespace Anteater.Intercom.Gui.Controls;

public partial class CallButton : FlexLayout, IRecipient<DoorLockStateChanged>
{
    public static readonly BindableProperty IsCallStartedProperty =
        BindableProperty.Create(nameof(IsCallStarted), typeof(bool), typeof(CallButton));

    private readonly IMessenger _messenger;
    private readonly IAudioRecord _recorder;
    private readonly IReversAudioService _reversAudio;

    private CancellationTokenSource _cts;

    public CallButton()
    {
        _messenger = App.Services.GetRequiredService<IMessenger>();
        _recorder = App.Services.GetRequiredService<IAudioRecord>();
        _reversAudio = App.Services.GetRequiredService<IReversAudioService>();

        _messenger.Register(this);

        InitializeComponent();

        _button.Pressed += OnPressed;
    }

    public bool IsCallStarted
    {
        get => GetValue(IsCallStartedProperty) as bool? ?? false;
        set => SetValue(IsCallStartedProperty, value);
    }

    void OnPressed(object sender, EventArgs e)
    {
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
                MainThread.BeginInvokeOnMainThread(delegate
                {
                    IsCallStarted = false;
                });

                return;
            }
        }

        _cts.Token.Register(delegate
        {
            MainThread.BeginInvokeOnMainThread(delegate
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
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _button.IsEnabled = message.IsLocked;
        });
    }
}
