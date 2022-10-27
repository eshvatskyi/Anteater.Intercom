using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Policy;
using System.Threading.Tasks;
using Anteater.Intercom.Device;
using Anteater.Intercom.Device.Rtsp;
using Anteater.Pipe;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using NAudio.Wave;

namespace Anteater.Intercom.Gui.Controls;

public partial class VideoPlayer : Grid
{
    private readonly IPipe _pipe;
    private readonly WaveOut _waveOut;
    private readonly RtspStreamReader _rtspStream;

    private ConnectionSettings _settings;

    public VideoPlayer()
    {
        _pipe = App.ServiceProvider.GetService<IPipe>();

        var connectionSettings = App.ServiceProvider.GetService<IOptionsMonitor<ConnectionSettings>>();

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

        var videoState = _pipe.HandleAsync<ChangeVideoState>(OnChangeVideoState);
        var soundStateChanged = _pipe.Subscribe<SoundStateChanged>(OnSoundStateChanged);

        _waveOut = new WaveOut();
        _rtspStream = new RtspStreamReader();

        InitializeComponent();

        void UnloadEventHandler()
        {
            settingsState.Dispose();
            videoState.Dispose();
            soundStateChanged.Dispose();
            _rtspStream.Dispose();
            _waveOut.Stop();
            _waveOut.Dispose();
        };

        Unloaded += (_, _) => UnloadEventHandler();
        MainWindow.Instance.Closed += (_, _) => UnloadEventHandler();
    }

    Task OnChangeVideoState(ChangeVideoState command) => Task.Run(() =>
    {
        DispatcherQueue.TryEnqueue(delegate
        {
            Visibility = command.IsPaused ? Microsoft.UI.Xaml.Visibility.Collapsed : Microsoft.UI.Xaml.Visibility.Visible;
        });

        if (command.IsPaused)
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
    });

    Task OnSoundStateChanged(SoundStateChanged @event)
    {
        if (@event.IsMuted)
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

        return Task.CompletedTask;
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

            _rtspStream.OnVideoFrameDecoded = data =>
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

        _rtspStream.OnAudioFrameDecoded = (data) =>
        {
            waveProvider.AddSamples(data, 0, data.Length);
        };

        _waveOut.Init(waveProvider);
        _waveOut.Play();
    }
}
