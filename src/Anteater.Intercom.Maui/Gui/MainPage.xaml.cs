using Anteater.Intercom.Gui.Messages;
using Anteater.Intercom.Services.Audio;
using Anteater.Intercom.Services.Rtsp;
using Anteater.Intercom.Services.Settings;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Options;
using SkiaSharp;
using SkiaSharp.Views.Maui.Controls;

namespace Anteater.Intercom.Gui;

public partial class MainPage : ContentPage, IRecipient<WindowStateChanged>
{
    private readonly RtspStreamReader _rtspStream;
    private readonly IAudioPlayback _playback;

    private ConnectionSettings _settings;
    private SKImageInfo _imageInfo;

    private bool _restartRtspReader = false;

    public MainPage()
    {
        var messenger = App.Services.GetService<IMessenger>();

        messenger.Register(this);

        var connectionSettings = App.Services.GetService<IOptionsMonitor<ConnectionSettings>>();

        _settings = connectionSettings.CurrentValue;

        var settingsState = connectionSettings.OnChange(settings =>
        {
            _settings = settings;

            _restartRtspReader = false;

            _ = Task.Run(Connect);
        });

        _playback = App.Services.GetService<IAudioPlayback>();

        _rtspStream = new RtspStreamReader();

        InitializeComponent();

        Loaded += delegate
        {
            _ = Task.Run(Connect);
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_restartRtspReader)
        {
            _ = Task.Run(_rtspStream.Start);
        }
    }

    protected override void OnDisappearing()
    {
        _restartRtspReader = true;

        _ = Task.Run(_rtspStream.Stop);

        base.OnDisappearing();
    }

    void Connect()
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
        MainThread.BeginInvokeOnMainThread(delegate
        {
            using var skdata = SKData.CreateCopy(data);
            using var origin = new SKBitmap();

            origin.InstallPixels(_imageInfo, skdata.Data);

            using var resize = origin.Resize(new SKSizeI((int)_image.Width, (int)_image.Height), SKFilterQuality.Medium);

            _image.Source = new SKBitmapImageSource { Bitmap = resize };
        });
    }

    void OnAudioFrameDecoded(RtspStream stream, byte[] data)
    {
        _playback.AddSamples(data);
    }

    void Settings_Pressed(object sender, EventArgs e)
    {
        Navigation.PushAsync(new Settings(), true);
    }

    public void Receive(WindowStateChanged message)
    {
        switch (message.State)
        {
            case WindowState.Resumed:
                if (_rtspStream.IsStopped && !_rtspStream.IsReconnecting)
                {
                    _ = Task.Run(_rtspStream.Start);
                }
                break;

            case WindowState.Stopped:
                if (!_rtspStream.IsStopped)
                {
                    _ = Task.Run(_rtspStream.Stop);
                }
                break;
        }
    }
}

