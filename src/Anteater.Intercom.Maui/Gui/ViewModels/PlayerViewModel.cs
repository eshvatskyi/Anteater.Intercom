using Anteater.Intercom.Services.Audio;
using Anteater.Intercom.Services.Rtsp;
using Anteater.Intercom.Services.Settings;
using Microsoft.Extensions.Options;
using SkiaSharp;
using SkiaSharp.Views.Maui.Controls;

namespace Anteater.Intercom.Gui.ViewModels;

public partial class PlayerViewModel : ObservableViewModelBase
{
    private RtspStreamReader _rtspStream;
    private readonly IAudioPlayback _playback;

    private ConnectionSettings _settings;

    private CancellationTokenSource _reconnectCancellation;
    private Task _mainThreadImageSourceTask = Task.CompletedTask;

    private SKImageInfo _imageInfo;
    private ImageSource _imageSource;
    private int _imageWidth;
    private int _imageHeight;
    private bool _isSoundMuted;

    public PlayerViewModel(IAudioPlayback playback, IOptionsMonitor<ConnectionSettings> connectionSettings)
    {
        _playback = playback;

        _settings = connectionSettings.CurrentValue;

        connectionSettings.OnChange(settings => _settings = settings);
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

    public void Connect()
    {
        ImageSource = ImageSource.FromFile("playeron.png");

        _rtspStream = new RtspStreamReader();

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

        if (format.Video is null)
        {
            _reconnectCancellation = new CancellationTokenSource();

            Task.Delay(TimeSpan.FromSeconds(5), _reconnectCancellation.Token)
                .ContinueWith(_ => Connect(), TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        InitializeVideo(format.Video);
        InitializeAudio(format.Audio);
    }

    public void Stop()
    {
        _reconnectCancellation?.Cancel();

        _rtspStream.VideoFrameDecoded -= OnVideoFrameDecoded;
        _rtspStream.AudioFrameDecoded -= OnAudioFrameDecoded;
        _rtspStream.Stop();
        _playback.Stop();

        _rtspStream = null;
        _imageInfo = SKImageInfo.Empty;

        ImageSource = ImageSource.FromFile("playeroff.png");
    }

    public void IsSoundMuted(bool state)
    {
        if (_isSoundMuted = state)
        {
            if (!_playback.IsStopped)
            {
                _rtspStream.AudioFrameDecoded -= OnAudioFrameDecoded;
                _ = Task.Run(_playback.Stop);
            }
        }
        else
        {
            if (_playback.IsStopped)
            {
                _ = Task.Run(_playback.Start);
                _rtspStream.AudioFrameDecoded += OnAudioFrameDecoded;
            }
        }
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

        if (!_isSoundMuted)
        {
            _playback.Start();
            _rtspStream.AudioFrameDecoded += OnAudioFrameDecoded;
        }
    }

    void OnVideoFrameDecoded(RtspStream stream, byte[] data)
    {
        try
        {
            if (_mainThreadImageSourceTask.IsCompleted)
            {
                _mainThreadImageSourceTask = MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (ImageSource is SKBitmapImageSource bitmapImageSource)
                    {
                        bitmapImageSource?.Bitmap?.Dispose();
                    }

                    if (_imageInfo.IsEmpty)
                    {
                        return;
                    }

                    using var skdata = SKData.CreateCopy(data);
                    using var origin = new SKBitmap();

                    if (origin.InstallPixels(_imageInfo, skdata.Data))
                    {
                        ImageSource = (SKBitmapImageSource)origin.Resize(new SKSizeI(ImageWidth, ImageHeight), SKFilterQuality.Medium);
                    }
                });
            }
        }
        catch { }
    }

    void OnAudioFrameDecoded(RtspStream stream, byte[] data)
    {
        _playback.AddSamples(data);
    }
}
