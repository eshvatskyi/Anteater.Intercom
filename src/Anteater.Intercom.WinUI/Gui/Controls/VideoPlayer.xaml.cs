using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Anteater.Intercom.Services.Audio;
using Anteater.Intercom.Services.Rtsp;
using Anteater.Intercom.Services.Settings;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Anteater.Intercom.Gui.Controls;

public partial class VideoPlayer : Grid, IRecipient<ChangeVideoState>, IRecipient<SoundStateChanged>
{
    private readonly IMessenger _messenger;
    private readonly ConnectionSettings _settings;
    private readonly IAudioPlayback _playback;
    private readonly RtspStreamReader _rtspStream;

    private bool _isMuted;
    private WriteableBitmap _bitmap;

    public VideoPlayer()
    {
        _messenger = App.Services.GetService<IMessenger>();

        _settings = App.Services.GetService<IOptionsMonitor<ConnectionSettings>>().CurrentValue;

        _playback = App.Services.GetService<IAudioPlayback>();
        _rtspStream = new RtspStreamReader();

        InitializeComponent();

        Loaded += OnLoaded;
        Unloaded += OnUnload;

        MainWindow.Instance.Closed += (_, _) => OnUnload(null, null);
    }

    void OnLoaded(object sender, RoutedEventArgs e)
    {
        _messenger.RegisterAll(this);

        _rtspStream.VideoFrameDecoded += OnVideoFrameDecoded;
        _rtspStream.AudioFrameDecoded += OnAudioFrameDecoded;
    }

    void OnUnload(object sender, RoutedEventArgs e)
    {
        _messenger.UnregisterAll(this);

        _rtspStream.VideoFrameDecoded -= OnVideoFrameDecoded;
        _rtspStream.AudioFrameDecoded -= OnAudioFrameDecoded;
        _rtspStream.Stop();

        _playback.Stop();
    }

    public void Connect()
    {
        _bitmap = null;

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
        if (format is null)
        {
            return;
        }

        DispatcherQueue.TryEnqueue(delegate
        {
            _bitmap = new WriteableBitmap(format.Width, format.Height);

            _image.Source = _bitmap;
        });
    }

    void InitializeAudio(RtspStreamAudioFormat format)
    {
        _playback.Stop();

        if (format is null)
        {
            return;
        }

        _playback.Init(format.SampleRate, format.Channels);

        if (!_isMuted)
        {
            _playback.Start();
        }
    }

    void OnVideoFrameDecoded(RtspStream stream, byte[] data)
    {
        if (_bitmap is not null)
        {
            DispatcherQueue?.TryEnqueue(delegate
            {
                data.CopyTo(_bitmap.PixelBuffer);

                _bitmap.Invalidate();
            });
        }
    }

    void OnAudioFrameDecoded(RtspStream stream, byte[] data)
    {
        _playback.AddSamples(data);
    }

    void IRecipient<ChangeVideoState>.Receive(ChangeVideoState message)
    {
        DispatcherQueue.TryEnqueue(delegate
        {
            Visibility = message.IsPaused ? Visibility.Collapsed : Visibility.Visible;
        });

        if (message.IsPaused)
        {
            if (!_rtspStream.IsStopped)
            {
                _ = Task.Run(_rtspStream.Stop);
            }
        }
        else
        {
            if (_rtspStream.IsStopped)
            {
                _ = Task.Run(_rtspStream.Start);
            }
        }
    }

    void IRecipient<SoundStateChanged>.Receive(SoundStateChanged message)
    {
        if (_isMuted = message.IsMuted)
        {
            if (!_playback.IsStopped)
            {
                _ = Task.Run(_playback.Stop);
            }
        }
        else
        {
            if (_playback.IsStopped)
            {
                _ = Task.Run(_playback.Start);
            }
        }
    }
}
