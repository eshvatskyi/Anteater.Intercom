using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Anteater.Intercom.Device;
using Anteater.Pipe;
using LibVLCSharp.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Anteater.Intercom.Gui.Controls
{
    public partial class VideoPlayer : Grid
    {
        private readonly IPipe _pipe;
        private readonly IOptionsMonitor<ConnectionSettings> _connectionSettings;

        private readonly LibVLC _libvlc;

        private uint _width;
        private uint _height;
        private uint _pitch;
        private WriteableBitmap _bitmap;
        private MemoryMappedFile _file;
        private MemoryMappedViewAccessor _accessor;
        private MediaPlayer _mediaPlayer;

        public VideoPlayer()
        {
            _pipe = App.ServiceProvider.GetService<IPipe>();

            var videoState = _pipe.HandleAsync<ChangeVideoState>(OnChangeVideoState);
            var soundStateChanged = _pipe.Subscribe<SoundStateChanged>(OnSoundStateChanged);

            _connectionSettings = App.ServiceProvider.GetService<IOptionsMonitor<ConnectionSettings>>();
            _connectionSettings.OnChange(Connect);

            Core.Initialize();

            _width = 960;
            _height = 576;
            _pitch = 960 * 4;

            _bitmap = new WriteableBitmap(Convert.ToInt32(_width), Convert.ToInt32(_height));
            _file = MemoryMappedFile.CreateNew(null, _pitch * _height);
            _accessor = _file.CreateViewAccessor();

            _libvlc = new LibVLC("--intf=dummy", "--vout=dummy");

            Connect(_connectionSettings.CurrentValue);

            InitializeComponent();

            _image.Source = _bitmap;

            void UnloadEventHandler()
            {
                videoState.Dispose();
                soundStateChanged.Dispose();
                _mediaPlayer.Stop();
                _mediaPlayer.Dispose();
                _libvlc.Dispose();
                _accessor.Dispose();
                _file.Dispose();
            };

            Unloaded += (_, _) => UnloadEventHandler();
            MainWindow.Instance.Closed += (_, _) => UnloadEventHandler();
        }

        Task OnChangeVideoState(ChangeVideoState command) => Task.Run(() =>
        {
            if (_mediaPlayer is null)
            {
                return;
            }

            DispatcherQueue.TryEnqueue(delegate
            {
                Visibility = command.IsPaused ? Microsoft.UI.Xaml.Visibility.Collapsed : Microsoft.UI.Xaml.Visibility.Visible;
            });

            if (command.IsPaused)
            {
                if (_mediaPlayer.IsPlaying)
                {
                    _mediaPlayer.Stop();
                }
            }
            else
            {
                if (!_mediaPlayer.IsPlaying)
                {
                    _mediaPlayer.Play();
                }
            }
        });

        Task OnSoundStateChanged(SoundStateChanged @event)
        {
            _mediaPlayer.Mute = @event.IsMuted;

            return Task.CompletedTask;
        }

        void Connect(ConnectionSettings settings)
        {
            if (_mediaPlayer is not null)
            {
                _mediaPlayer.Stop();
                _mediaPlayer.Dispose();
            }

            var uri = new Uri($"rtsp://{settings.Username}:{settings.Password}@{settings.Host}:{settings.RtspPort}/av0_0");

            _mediaPlayer = new MediaPlayer(new Media(_libvlc, uri));
            _mediaPlayer.EnableHardwareDecoding = true;
            _mediaPlayer.SetVideoFormat("BGRA", _width, _height, _pitch);
            _mediaPlayer.SetVideoCallbacks(VideoLock, null, VideoDisplay);
            _mediaPlayer.Play();
        }

        IntPtr VideoLock(IntPtr opaque, IntPtr planes)
        {
            try
            {
                Marshal.WriteIntPtr(planes, _accessor.SafeMemoryMappedViewHandle.DangerousGetHandle());
            }
            catch { }

            return IntPtr.Zero;
        }

        void VideoDisplay(IntPtr opaque, IntPtr picture)
        {
            try
            {
                var stream = _file.CreateViewStream();

                DispatcherQueue.TryEnqueue(delegate
                {
                    using var sourceStream = _bitmap.PixelBuffer.AsStream();

                    stream.CopyTo(sourceStream);

                    _bitmap.Invalidate();
                });
            }
            catch { }
        }
    }
}
