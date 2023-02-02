using System.ComponentModel;
using System.Runtime.CompilerServices;
using Anteater.Intercom.Gui.Messages;
using Anteater.Intercom.Services.Audio;
using Anteater.Intercom.Services.ReversChannel;
using Anteater.Intercom.Services.Rtsp;
using Anteater.Intercom.Services.Settings;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Options;
using SkiaSharp;
using SkiaSharp.Views.Maui.Controls;

namespace Anteater.Intercom.Gui.ViewModels;

public class IntercomViewModel : INotifyPropertyChanged, IRecipient<WindowStateChanged>
{
    private readonly RtspStreamReader _rtspStream;
    private readonly IAudioPlayback _playback;
    private readonly IAudioRecord _recorder;
    private readonly IDoorLockService _doorLock;
    private readonly IReversAudioService _reversAudio;

    private ConnectionSettings _settings;

    private SKImageInfo _imageInfo;
    private ImageSource _imageSource;
    private int _imageWidth;
    private int _imageHeight;

    private bool _isDoorLocked = true;
    private bool _isCallStarted = false;

    private CancellationTokenSource _callStopTokenSource;

    public event PropertyChangedEventHandler PropertyChanged;

    public IntercomViewModel(IMessenger messenger, IAudioPlayback playback, IAudioRecord recorder, IDoorLockService doorLock, IReversAudioService reversAudio, IOptionsMonitor<ConnectionSettings> connectionSettings)
    {
        _rtspStream = new RtspStreamReader();
        _playback = playback;
        _recorder = recorder;
        _reversAudio = reversAudio;
        _doorLock = doorLock;

        _settings = connectionSettings.CurrentValue;

        connectionSettings.OnChange(settings => _settings = settings);

        UnlockDoor = new RelayCommand(UnlokDoorCommand);

        StartCall = new RelayCommand(StartCallCommand);

        messenger.RegisterAll(this);
    }

    public int ImageWidth
    {
        get => _imageWidth;
        set => SetProperty(ref _imageWidth, value);
    }

    public int ImageHeight
    {
        get => _imageHeight;
        set => SetProperty(ref _imageHeight, value);
    }

    public ImageSource ImageSource
    {
        get => _imageSource;
        set => SetProperty(ref _imageSource, value);
    }

    public bool IsDoorLocked
    {
        get => _isDoorLocked;
        set => SetProperty(ref _isDoorLocked, value);
    }

    public bool IsCallStarted
    {
        get => _isCallStarted;
        set => SetProperty(ref _isCallStarted, value);
    }

    public IRelayCommand UnlockDoor { get; }

    public IRelayCommand StartCall { get; }

    public void Connect()
    {
        var uriBuilder = new UriBuilder
        {
            Scheme = "rtsp",
            Host = _settings.Host,
            Port = _settings.RtspPort,
            UserName = _settings.Username,
            Password = _settings.Password,
            Path = "av0_0",
        };

        var format = _rtspStream.Start(uriBuilder.Uri.ToString());

        InitializeVideo(format.Video);
        InitializeAudio(format.Audio);
    }

    public void Stop()
    {
        _rtspStream.VideoFrameDecoded -= OnVideoFrameDecoded;
        _rtspStream.AudioFrameDecoded -= OnAudioFrameDecoded;
        _rtspStream.Stop();
        _playback.Stop();
    }

    void InitializeVideo(RtspStreamVideoFormat format)
    {
        _rtspStream.VideoFrameDecoded -= OnVideoFrameDecoded;

        if (format is null)
        {
            return;
        }

        _imageInfo = new SKImageInfo(format.Width, format.Height, SKColorType.Rgba8888);

        _rtspStream.VideoFrameDecoded += OnVideoFrameDecoded;
    }

    void InitializeAudio(RtspStreamAudioFormat format)
    {
        _rtspStream.AudioFrameDecoded -= OnAudioFrameDecoded;

        _playback.Stop();

        if (format is null)
        {
            return;
        }

        _playback.Init(format.SampleRate, format.Channels);

        _playback.Start();

        _rtspStream.AudioFrameDecoded += OnAudioFrameDecoded;
    }

    void OnVideoFrameDecoded(RtspStream stream, byte[] data)
    {
        try
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                using var skdata = SKData.CreateCopy(data);
                using var origin = new SKBitmap();

                origin.InstallPixels(_imageInfo, skdata.Data);

                using var resize = origin.Resize(new SKSizeI(ImageWidth, ImageHeight), SKFilterQuality.Medium);

                ImageSource = new SKBitmapImageSource { Bitmap = resize };
            });
        }
        catch { }
    }

    void OnAudioFrameDecoded(RtspStream stream, byte[] data)
    {
        _playback.AddSamples(data);
    }

    void UnlokDoorCommand()
    {
        _ = Task.Run(async () =>
        {
            if (IsDoorLocked)
            {
                IsDoorLocked = false;

                try
                {
                    await _doorLock.UnlockDoorAsync();
                }
                catch { }

                IsDoorLocked = true;
            }
        });
    }

    void StartCallCommand()
    {
        _ = Task.Run(async () =>
        {
            if (IsCallStarted = !IsCallStarted)
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
                await _reversAudio.ConnectAsync(_recorder.Format, _recorder.SampleRate, _recorder.Channels);
            }
            catch
            {
                IsCallStarted = false;

                return;
            }
        }

        _callStopTokenSource.Token.Register(() =>
        {
            IsCallStarted = false;

            if (disconnect)
            {
                _reversAudio.Disconnect();
            }

            _recorder.DataAvailable -= OnRecordingDataAvailable;

            _recorder.Stopped -= OnRecordingStopped;

            _recorder.Stop();
        });


        var tcs = new TaskCompletionSource();

        _callStopTokenSource.Token.Register(() =>
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
            _callStopTokenSource.Cancel();
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
            _callStopTokenSource?.Cancel();
        }
    }

    void OnRecordingStopped()
    {
        _callStopTokenSource.Cancel();
    }

    bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
    {
        if (Equals(storage, value))
        {
            return false;
        }

        storage = value;

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        return true;
    }

    void IRecipient<WindowStateChanged>.Receive(WindowStateChanged message)
    {
        switch (message.State)
        {
            case WindowState.Resumed:
                if (_rtspStream.IsStopped && !_rtspStream.IsReconnecting)
                {
                    _ = Task.Run(Connect);
                }
                break;

            case WindowState.Stopped:
                if (!_rtspStream.IsStopped)
                {
                    _ = Task.Run(Stop);
                }
                break;

            case WindowState.Closing:
                _ = Task.Run(Stop);
                break;
        }
    }
}
