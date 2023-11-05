using Anteater.Intercom.Core.Audio;
using Anteater.Intercom.Core.Rtsp;
using Anteater.Intercom.Core.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using SkiaSharp;
using SkiaSharp.Views.Maui.Controls;

namespace Anteater.Intercom.Features.Intercom;

public partial class PlayerViewModel : ObservableObject
{
    private RtspStreamContext _rtspStream;
    private readonly IAudioPlayback _playback;
    private readonly ISettingsService _settings;

    private CancellationTokenSource _reconnectCancellation;
    private Task _mainThreadImageSourceTask = Task.CompletedTask;

    private SKImageInfo _imageInfo;
    private ImageSource _imageSource;
    private int _imageWidth;
    private int _imageHeight;
    private bool _isSoundMuted;
    private int _reconnectAttempt = 0;

    public PlayerViewModel(IAudioPlayback playback, ISettingsService settings)
    {
        _playback = playback;
        _settings = settings;
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
            Host = _settings.Current.Host,
            Port = _settings.Current.RtspPort,
            UserName = _settings.Current.Username,
            Password = _settings.Current.Password,
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

        _playback.Stop();

        MainThread.InvokeOnMainThreadAsync(() =>
        {
            ImageSource = ImageSource.FromFile("playeroff.png");
        });
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

                    if (_imageInfo.IsEmpty)
                    {
                        return;
                    }

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

        var reconnectTimeout = TimeSpan.FromSeconds(_reconnectAttempt++ switch
        {
            1 => 3,
            2 => 5,
            _ => 10
        });

        _reconnectCancellation = new CancellationTokenSource();

        _reconnectCancellation.Token.Register(() => _reconnectAttempt = 0);

        Task.Delay(reconnectTimeout, _reconnectCancellation.Token)
            .ContinueWith(_ => Connect(), TaskContinuationOptions.OnlyOnRanToCompletion);
    }
}
