using System;
using System.Runtime.InteropServices.WindowsRuntime;
using Anteater.Intercom.Services;
using Anteater.Intercom.Services.Rtsp;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using NAudio.Wave;

namespace Anteater.Intercom.Gui.Controls;

public partial class VideoPlayer : Grid,
    IRecipient<ChangeVideoState>,
    IRecipient<SoundStateChanged>
{
    private readonly IMessenger _messenger;
    private readonly WaveOut _waveOut;
    private readonly RtspStreamReader _rtspStream;

    private ConnectionSettings _settings;

    public VideoPlayer()
    {
        _messenger = App.Services.GetService<IMessenger>();

        _messenger.RegisterAll(this);

        var connectionSettings = App.Services.GetService<IOptionsMonitor<ConnectionSettings>>();

        _settings = connectionSettings.CurrentValue;

        var settingsState = connectionSettings.OnChange(settings =>
        {
            if (_settings == settings)
            {
                return;
            }

            _settings = settings;

            Connect();
        });

        _waveOut = new WaveOut();
        _rtspStream = new RtspStreamReader();

        InitializeComponent();

        void UnloadEventHandler()
        {
            settingsState.Dispose();
            _rtspStream.Dispose();
            _waveOut.Stop();
            _waveOut.Dispose();
            _messenger.UnregisterAll(this);
        };

        Unloaded += (_, _) => UnloadEventHandler();
        MainWindow.Instance.Closed += (_, _) => UnloadEventHandler();
    }

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

    void InitializeVideo(RtspStreamVideoFormat format)
    {
        if (format is null)
        {
            return;
        }

        DispatcherQueue.TryEnqueue(delegate
        {
            var bitmap = new WriteableBitmap(format.Width, format.Height);

            _rtspStream.VideoFrameDecoded += (_, data) =>
            {
                DispatcherQueue?.TryEnqueue(delegate
                {
                    data.CopyTo(bitmap.PixelBuffer);

                    bitmap.Invalidate();
                });
            };

            _image.Source = bitmap;
        });
    }

    void InitializeAudio(RtspStreamAudioFormat format)
    {
        _waveOut.Stop();

        if (format is null)
        {
            return;
        }

        var waveFormat = new WaveFormat(format.SampleRate, format.Channels);

        var waveProvider = new BufferedWaveProvider(waveFormat)
        {
            BufferLength = format.SampleRate / 2,
            DiscardOnBufferOverflow = true
        };

        _rtspStream.AudioFrameDecoded += (_, data) =>
        {
            waveProvider.AddSamples(data, 0, data.Length);
        };

        _waveOut.Init(waveProvider);
        _waveOut.Play();
    }

    void IRecipient<ChangeVideoState>.Receive(ChangeVideoState message)
    {
        DispatcherQueue.TryEnqueue(delegate
        {
            Visibility = message.IsPaused ? Microsoft.UI.Xaml.Visibility.Collapsed : Microsoft.UI.Xaml.Visibility.Visible;
        });

        if (message.IsPaused)
        {
            if (!_rtspStream.IsStopped)
            {
                _rtspStream.Stop();
            }
        }
        else
        {
            if (_rtspStream.IsStopped)
            {
                _rtspStream.Start();
            }
        }
    }

    void IRecipient<SoundStateChanged>.Receive(SoundStateChanged message)
    {
        if (message.IsMuted)
        {
            if (_waveOut.PlaybackState == PlaybackState.Playing)
            {
                _waveOut.Pause();
            }
        }
        else
        {
            if (_waveOut.PlaybackState != PlaybackState.Playing)
            {
                _waveOut.Play();
            }
        }
    }
}
