using System;
using System.Drawing;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Anteater.Intercom.Device.Events;
using Anteater.Intercom.Device.Rtsp;
using NAudio.Wave;
using RtspClientSharp.Decoding;

namespace Anteater.Intercom.Gui.ViewModel
{
    public class VideoPlayerViewModel : BaseViewModel
    {
        private static readonly Lazy<(double DpiX, double DpiY)> ScreenDpi = new Lazy<(double, double)>(() =>
        {
            using var g = Graphics.FromHwnd(IntPtr.Zero);
            return (g.DpiX, g.DpiY);
        });

        private readonly RtspDataService _rtsp;
        private readonly IDisposable _disposable;

        private IDisposable _hideVideoTimer;

        private bool _isSoundMuted;
        private ImageSource _imageSource;

        private TransformParameters _transformParameters;
        private Int32Rect _dirtyRect;
        private WriteableBitmap _writeableBitmap;

        public VideoPlayerViewModel(AlarmEventsService alarmEvents, RtspDataService rtsp)
        {
            _rtsp = rtsp;

            IsSoundMuted = true;

            AudioStateCommand = new RelayCommand(ChangeAudioState);
            VideoStateCommand = new RelayCommand<bool>(ChangeVideoState);

            var audioSubscription = InitAudio(rtsp);
            var videoSubscription = InitVideo(rtsp);
            var eventsSubscription = InitAlarmEvents(alarmEvents);

            _disposable = Disposable.Create(() =>
            {
                audioSubscription.Dispose();
                videoSubscription.Dispose();
                eventsSubscription.Dispose();
            });
        }

        public ICommand AudioStateCommand { get; }

        public ICommand VideoStateCommand { get; }

        public bool IsSoundMuted
        {
            get => _isSoundMuted;
            set => SetProperty(ref _isSoundMuted, value);
        }

        public ImageSource Source
        {
            get => _imageSource;
            set => SetProperty(ref _imageSource, value);
        }

        public void Resize(int width, int height)
        {
            var initialization = _transformParameters == null;

            _dirtyRect = new Int32Rect(0, 0, width, height);

            _transformParameters = new TransformParameters(
                RectangleF.Empty,
                new System.Drawing.Size(width, height),
                ScalingPolicy.Stretch,
                RtspClientSharp.Decoding.PixelFormat.Bgra32,
                ScalingQuality.FastBilinear);

            _writeableBitmap = new WriteableBitmap(width, height, ScreenDpi.Value.DpiX, ScreenDpi.Value.DpiY, PixelFormats.Pbgra32, null);

            RenderOptions.SetBitmapScalingMode(_writeableBitmap, BitmapScalingMode.NearestNeighbor);

            _writeableBitmap.Lock();

            try
            {
                _writeableBitmap.AddDirtyRect(_dirtyRect);
            }
            finally
            {
                _writeableBitmap.Unlock();
            }

            if (initialization || Source != null)
            {
                Source = _writeableBitmap;
            }
        }

        void ChangeAudioState()
        {
            IsSoundMuted = !IsSoundMuted;

            _rtsp.SetAudioState(IsSoundMuted);
        }

        void ChangeVideoState(bool stopped)
        {
            _hideVideoTimer?.Dispose();

            if (stopped)
            {
                _hideVideoTimer = Observable
                    .Timer(TimeSpan.FromSeconds(30))
                    .Subscribe(_ =>
                    {
                        _rtsp.SetAudioState(true);
                        _rtsp.SetVideoState(true);
                        Source = null;
                    });
            }
            else
            {
                _rtsp.SetAudioState(IsSoundMuted);
                _rtsp.SetVideoState(false);
                Source = _writeableBitmap;
            }
        }

        IDisposable InitAudio(RtspDataService rtsp)
        {
            var waveOut = new WaveOut();
            var waveFormat = new WaveFormat(8000, 1);
            var waveProvider = new BufferedWaveProvider(waveFormat);
            waveOut.Init(waveProvider);
            waveOut.Play();

            var subscription = rtsp.AsAudioObservable().Subscribe(frame =>
            {
                try
                {
                    var sample = frame.DecodedBytes.ToArray();

                    waveProvider.AddSamples(sample, 0, sample.Length);
                }
                catch { }
            });

            return Disposable.Create(() =>
            {
                waveOut.Dispose();
                subscription.Dispose();
            });
        }

        IDisposable InitVideo(RtspDataService rtsp)
        {
            return rtsp.AsVideoObservable()
                .ObserveOnDispatcher(DispatcherPriority.Send)
                .Where(x => _writeableBitmap != null)
                .Subscribe(frame =>
                {
                    _writeableBitmap.Lock();

                    try
                    {
                        frame.TransformTo(_writeableBitmap.BackBuffer, _writeableBitmap.BackBufferStride, _transformParameters);

                        _writeableBitmap.AddDirtyRect(_dirtyRect);
                    }
                    finally
                    {
                        _writeableBitmap.Unlock();
                    }
                });
        }

        IDisposable InitAlarmEvents(AlarmEventsService alarmEvents)
        {
            return alarmEvents.AsObservable()
                .Where(e => e.Status && e.Type switch
                {
                    AlarmEvent.EventType.MotionDetection => true,
                    AlarmEvent.EventType.SensorAlarm => true,
                    _ => false
                })
                .ObserveOnDispatcher(DispatcherPriority.Send)
                .Subscribe(_ =>
                {
                    _rtsp.SetVideoState(false);
                    Source = _writeableBitmap;
                });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _hideVideoTimer?.Dispose();
                _disposable?.Dispose();
            }
        }
    }
}
