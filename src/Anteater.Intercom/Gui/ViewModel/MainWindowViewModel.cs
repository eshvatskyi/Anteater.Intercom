using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using System.Windows.Threading;
using Anteater.Intercom.Device.Events;

namespace Anteater.Intercom.Gui.ViewModel
{
    public class MainWindowViewModel : BaseViewModel
    {
        private readonly IDisposable _disposable;

        private IDisposable _hideOverlayTimer;

        private bool _isMaximized;
        private bool _isOverlayVisible;

        public MainWindowViewModel(AlarmEventsService alarmEvents)
        {
            ChangeWindowStateCommand = new RelayCommand(ChangeWindowState);
            ChangeOverlayStateCommand = new RelayCommand<bool>(ChangeOverlayState);
            ChangeOverlayTimerStateCommand = new RelayCommand<bool>(ChangeOverlayTimerState);

            ChangeOverlayTimerState(false);

            var eventsSubscription = InitAlarmEvents(alarmEvents);

            _disposable = Disposable.Create(() =>
            {
                eventsSubscription.Dispose();
            });
        }

        public ICommand ChangeWindowStateCommand { get; }

        public ICommand ChangeOverlayStateCommand { get; }

        public ICommand ChangeOverlayTimerStateCommand { get; }

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
                _hideOverlayTimer?.Dispose();
                _disposable?.Dispose();
            }
        }
    }
}
