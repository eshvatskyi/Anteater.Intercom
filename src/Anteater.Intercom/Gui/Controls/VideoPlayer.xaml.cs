using System;
using System.Runtime.InteropServices.WindowsRuntime;
using Anteater.Intercom.Services;
using Anteater.Intercom.Services.Rtsp;
using CommunityToolkit.Mvvm.Messaging;
using CSCore;
using CSCore.SoundOut;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Anteater.Intercom.Gui.Controls;

public partial class VideoPlayer : Grid, IWaveSource, IRecipient<ChangeVideoState>, IRecipient<SoundStateChanged>
{
    private readonly IMessenger _messenger;
    private readonly WaveOut _playback;
    private readonly RtspStreamReader _rtspStream;

    private ConnectionSettings _settings;
    private QueuedBuffer _buffer;

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

        _playback = new WaveOut();
        _rtspStream = new RtspStreamReader();

        InitializeComponent();

        void UnloadEventHandler()
        {
            settingsState.Dispose();
            _rtspStream.Dispose();
            _playback.Stop();
            _playback.Dispose();
            _messenger.UnregisterAll(this);
        };

        Unloaded += (_, _) => UnloadEventHandler();
        MainWindow.Instance.Closed += (_, _) => UnloadEventHandler();
    }

    public bool CanSeek { get; } = false;

    public WaveFormat WaveFormat { get; private set; }

    public long Position { get; set; }

    public long Length => _buffer.Length;

    public int Read(byte[] buffer, int offset, int count)
    {
        var num = _buffer.Read(buffer, offset, count);

        if (num < count)
        {
            buffer.AsSpan().Slice(offset + num, count - num).Clear();

            num = count;
        }

        return num;
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
        _playback.Stop();

        if (format is null)
        {
            return;
        }

        _buffer = new QueuedBuffer(format.SampleRate / 2);

        WaveFormat = new WaveFormat(format.SampleRate, 16, format.Channels);

        _playback.Initialize(this);

        _rtspStream.AudioFrameDecoded += (_, data) =>
        {
            _buffer.Write(data, 0, data.Length);
        };

        _playback.Play();
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
            if (_playback.PlaybackState == PlaybackState.Playing)
            {
                _playback.Pause();
            }
        }
        else
        {
            if (_playback.PlaybackState != PlaybackState.Playing)
            {
                _playback.Play();
            }
        }
    }

    public void Dispose() { }
}
