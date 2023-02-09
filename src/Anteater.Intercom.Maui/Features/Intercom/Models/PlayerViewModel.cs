using Anteater.Intercom.Core;
using Anteater.Intercom.Services.Audio;
using Anteater.Intercom.Services.Rtsp;
using Anteater.Intercom.Services.Settings;
using Microsoft.Extensions.Options;
using SkiaSharp;
using SkiaSharp.Views.Maui.Controls;

namespace Anteater.Intercom.Features.Intercom;

public partial class PlayerViewModel : ObservableViewModelBase
{
    private RtspStreamContext _rtspStream;
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

    public bool IsConnected => _rtspStream is not null;

    public void Connect()
    {
        ImageSource = ImageSource.FromFile("playeron.png");

        var uriBuilder = new UriBuilder
        {
            Scheme = "rtsp",
            Host = _settings.Host,
            Port = _settings.RtspPort,
            UserName = _settings.Username,
            Password = _settings.Password,
            Path = "av0_0",
        };

        try
        {
            _rtspStream = RtspStreamContext.Create(uriBuilder.Uri.ToString());

            _rtspStream.FormatAvailable += (_, format) =>
            {
                _reconnectCancellation?.Cancel();

                InitializeVideo(format.Video);
                InitializeAudio(format.Audio);
            };

            _rtspStream.Stopped += OnStopped;
        }
        catch
        {
            OnStopped(_rtspStream);
        }
    }

    public void Stop()
    {
        _reconnectCancellation?.Cancel();

        _imageInfo = SKImageInfo.Empty;

        if (_rtspStream is not null)
        {
            _rtspStream.VideoFrameDecoded -= OnVideoFrameDecoded;
            _rtspStream.AudioFrameDecoded -= OnAudioFrameDecoded;
            _rtspStream.Stopped -= OnStopped;
            _rtspStream.DisposeAsync();
            _rtspStream = null;
        }

        _mainThreadImageSourceTask.ContinueWith(_ => ImageSource = ImageSource.FromFile("playeroff.png"));

        _playback.Stop();
    }

    public void IsSoundMuted(bool state)
    {
        if (_isSoundMuted = state)
        {
            if (!_playback.IsStopped)
            {
                if (_rtspStream is not null)
                {
                    _rtspStream.AudioFrameDecoded -= OnAudioFrameDecoded;
                }

                _playback.Stop();
            }
        }
        else
        {
            if (_playback.IsStopped)
            {
                _playback.Start();

                if (_rtspStream is not null)
                {
                    _rtspStream.AudioFrameDecoded += OnAudioFrameDecoded;
                }
            }
        }
    }

    void InitializeVideo(RtspStreamVideoFormat format)
    {
        if (format is null)
        {
            return;
        }

        _imageInfo = new SKImageInfo(format.Width, format.Height, SKColorType.Rgba8888);

        _rtspStream.VideoFrameDecoded += OnVideoFrameDecoded;
    }

    void InitializeAudio(RtspStreamAudioFormat format)
    {
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
        if (_imageInfo.IsEmpty)
        {
            return;
        }

        if (_mainThreadImageSourceTask.IsCompleted)
        {
            var skdata = SKData.CreateCopy(data);

            _mainThreadImageSourceTask = MainThread.InvokeOnMainThreadAsync(() =>
            {
                using (skdata)
                {
                    if (_imageInfo.IsEmpty)
                    {
                        return;
                    }

                    using var origin = new SKBitmap();

                    if (!origin.InstallPixels(_imageInfo, skdata.Data))
                    {
                        return;
                    }

                    using var resized = origin.Resize(new SKSizeI(ImageWidth, ImageHeight), SKFilterQuality.Medium);

                    ImageSource = (SKBitmapImageSource)resized;
                }
            });
        }
    }

    void OnAudioFrameDecoded(RtspStream stream, byte[] data)
    {
        _playback.AddSamples(data);
    }

    void OnStopped(RtspStreamContext context)
    {
        if (context is not null)
        {
            context.VideoFrameDecoded -= OnVideoFrameDecoded;
            context.AudioFrameDecoded -= OnAudioFrameDecoded;
            context.Stopped -= OnStopped;
        }

        _reconnectCancellation = new CancellationTokenSource();

        Task.Delay(TimeSpan.FromSeconds(10), _reconnectCancellation.Token)
            .ContinueWith(_ => Connect(), TaskContinuationOptions.OnlyOnRanToCompletion);
    }
}
