using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using System.Windows.Threading;
using Anteater.Intercom.Device;
using Anteater.Intercom.Device.Events;
using LibVLCSharp.Shared;

namespace Anteater.Intercom.Gui.ViewModel
{
    public class MainWindowViewModel : BaseViewModel
    {
        private readonly IDisposable _disposable;
        private readonly MediaPlayer _mediaPlayer;

        private IDisposable _hideOverlayTimer;
        private IDisposable _hideVideoTimer;
        private bool _isSoundMuted;

        private bool _isMaximized;
        private bool _isOverlayVisible;

        public MainWindowViewModel() { }

        public MainWindowViewModel(AlarmEventsService alarmEvents)
        {
            Core.Initialize();

            using var libvlc = new LibVLC();
            using var media = new Media(libvlc, new Uri($"rtsp://{ConnectionSettings.Default.Username}:{ConnectionSettings.Default.Password}@{ConnectionSettings.Default.Host}:554/av0_0"));

            _mediaPlayer = new MediaPlayer(media)
            {
                EnableHardwareDecoding = true,
                EnableKeyInput = false,
                EnableMouseInput = false
            };

            Intercom = new IntercomViewModel(alarmEvents, _mediaPlayer);
            AlarmRinger = new AlarmRingerViewModel(alarmEvents);

            IsSoundMuted = true;

            ChangeWindowStateCommand = new RelayCommand(ChangeWindowState);
            ChangeOverlayStateCommand = new RelayCommand<bool>(ChangeOverlayState);
            ChangeOverlayTimerStateCommand = new RelayCommand<bool>(ChangeOverlayTimerState);
            AudioStateCommand = new RelayCommand(ChangeAudioState);
            VideoStateCommand = new RelayCommand<bool>(ChangeVideoState);

            ChangeOverlayTimerState(false);

            var eventsSubscription = InitAlarmEvents(alarmEvents);

            _disposable = Disposable.Create(() =>
            {
                eventsSubscription.Dispose();
            });
        }

        public IntercomViewModel Intercom { get; }

        public AlarmRingerViewModel AlarmRinger { get; }

        public ICommand ChangeWindowStateCommand { get; }

        public ICommand ChangeOverlayStateCommand { get; }

        public ICommand ChangeOverlayTimerStateCommand { get; }

        public ICommand AudioStateCommand { get; }

        public ICommand VideoStateCommand { get; }

        public bool IsMaximized
        {
            get => _isMaximized;
            set => SetProperty(ref _isMaximized, value);
        }

        public bool IsOverlayVisible
        {
            get => _isOverlayVisible;
            set => SetProperty(ref _isOverlayVisible, value);
        }

        public bool IsSoundMuted
        {
            get => _isSoundMuted;
            set => SetProperty(ref _isSoundMuted, value);
        }

        public MediaPlayer MediaPlayer => _mediaPlayer;

        void ChangeAudioState()
        {
            IsSoundMuted = !IsSoundMuted;

            _mediaPlayer.Mute = IsSoundMuted;
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
                        _mediaPlayer.Mute = true;
                        _mediaPlayer.Stop();
                        RaisePropertyChanged(nameof(MediaPlayer));
                    });
            }
            else
            {
                _mediaPlayer.Mute = IsSoundMuted;
                _mediaPlayer.Play();
                RaisePropertyChanged(nameof(MediaPlayer));
            }
        }

        private void ChangeWindowState()
        {
            IsMaximized = !IsMaximized;

            ChangeOverlayState(true);
        }

        void ChangeOverlayState(bool visible)
        {
            IsOverlayVisible = visible;

            if (visible)
            {
                _mediaPlayer.Mute = IsSoundMuted;
                _mediaPlayer.Play();

                ChangeOverlayTimerState(false);
            }
        }

        void ChangeOverlayTimerState(bool stopped)
        {
            _hideOverlayTimer?.Dispose();

            if (!stopped)
            {
                _hideOverlayTimer = Observable
                   .Timer(TimeSpan.FromSeconds(15))
                   .Subscribe(_ => IsOverlayVisible = false);
            }
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
                .Select(_ => true)
                .Subscribe(ChangeOverlayState);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Intercom?.Dispose();
                AlarmRinger?.Dispose();

                _hideOverlayTimer?.Dispose();
                _disposable?.Dispose();
                _mediaPlayer?.Dispose();
            }
        }
    }
}
