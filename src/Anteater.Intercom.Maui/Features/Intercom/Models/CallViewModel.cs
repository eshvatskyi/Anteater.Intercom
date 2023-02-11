using Anteater.Intercom.Core;
using Anteater.Intercom.Services.Audio;
using Anteater.Intercom.Services.ReversChannel;
using CommunityToolkit.Mvvm.Input;
using FFmpeg.AutoGen.Abstractions;

namespace Anteater.Intercom.Features.Intercom;

public partial class CallViewModel : ObservableViewModelBase
{
    private readonly IAudioRecord _recorder;
    private readonly IReversAudioService _reversAudio;

    private bool _isStarted = false;

    private CancellationTokenSource _callStopTokenSource;

    public CallViewModel(IAudioRecord recorder, IReversAudioService reversAudio)
    {
        _recorder = recorder;
        _reversAudio = reversAudio;

        Start = new RelayCommand(StartCommand);
    }

    public bool IsStarted
    {
        get => _isStarted;
        set => SetProperty(ref _isStarted, value);
    }

    public IRelayCommand Start { get; }

    void StartCommand()
    {
        _ = Task.Run(async () =>
        {
            if (IsStarted = !IsStarted)
            {
                _callStopTokenSource = new CancellationTokenSource();

                try
                {
                    await StartCallAsync();
                }
                catch { }
            }
            else
            {
                _callStopTokenSource?.Cancel();
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
                await _reversAudio.ConnectAsync();
            }
            catch
            {
                IsStarted = false;

                return;
            }
        }

        _callStopTokenSource.Token.Register(() =>
        {
            IsStarted = false;

            if (disconnect)
            {
                _reversAudio.Disconnect();
            }

            _recorder.DataAvailable -= OnRecordingDataAvailable;

            _recorder.Stopped -= OnRecordingStopped;

            _recorder.Stop();
        });


        var tcs = new TaskCompletionSource();

        _callStopTokenSource.Token.Register(() => tcs.TrySetResult());

        _recorder.DataAvailable += OnRecordingDataAvailable;

        _recorder.Stopped += OnRecordingStopped;

        try
        {
            _recorder.Start();
        }
        catch
        {
            _callStopTokenSource.Cancel();
        }

        await tcs.Task;
    }

    async void OnRecordingDataAvailable(AVSampleFormat format, int sampleRate, int channels, byte[] data)
    {
        try
        {
            await _reversAudio.SendAsync(format, sampleRate, channels, data);
        }
        catch
        {
            _callStopTokenSource?.Cancel();
        }
    }

    void OnRecordingStopped()
    {
        _callStopTokenSource.Cancel();
    }
}
