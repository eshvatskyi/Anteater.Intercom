using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using System.Windows.Threading;
using Alanta.Client.Media;
using Alanta.Client.Media.Dsp.WebRtc;
using Anteater.Intercom.Device.Audio;
using Anteater.Intercom.Device.Events;
using Anteater.Intercom.Device.Rtsp;
using NAudio.Wave;

namespace Anteater.Intercom.Gui.ViewModel
{
    public class IntercomViewModel : BaseViewModel
    {
        private readonly RtspDataService _rtsp;
        private readonly BackChannelConnection _backChannelConnection;

        private bool _isDoorLocked;
        private bool _isActive;

        private IDisposable _activeCall;

        public IntercomViewModel(RtspDataService rtsp, AlarmEventsService alarmEvents)
        {
            _rtsp = rtsp ?? throw new ArgumentNullException(nameof(rtsp));

            _backChannelConnection = new BackChannelConnection(alarmEvents);

            IsDoorLocked = true;
            IsActive = false;

            ChangeLockStateCommand = new RelayCommand(ChangeLockState);

            ChangeCallStateCommand = new RelayCommand(ChangeCallState);

            alarmEvents.AsObservable()
                .Where(x => x.Type == AlarmEvent.EventType.SensorAlarm && x.Status)
                .ObserveOnDispatcher(DispatcherPriority.Send)
                .Subscribe(e =>
                {
                    IsActive = false;
                    _activeCall?.Dispose();
                });
        }

        public ICommand ChangeLockStateCommand { get; }

        public ICommand ChangeCallStateCommand { get; }

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public bool IsDoorLocked
        {
            get => _isDoorLocked;
            set => SetProperty(ref _isDoorLocked, value);
        }

        void ChangeLockState()
        {
            if (IsDoorLocked)
            {
                IsDoorLocked = false;

                _backChannelConnection.UnlockDoor()
                    .ObserveOnDispatcher(DispatcherPriority.Send)
                    .Subscribe(_ => IsDoorLocked = true);
            }
        }

        void ChangeCallState()
        {
            if (!IsActive)
            {
                IsActive = true;

                var disconnect = _backChannelConnection.IsOpen == false;

                if (!_backChannelConnection.IsOpen)
                {
                    _backChannelConnection.Connect();
                }

                var waveIn = new WaveInEvent
                {
                    WaveFormat = new WaveFormat(8000, 1),
                    BufferMilliseconds = 20
                };

                var echoCanceller = new WebRtcFilter(240, 100, new AudioFormat(), new AudioFormat(), true, true, true);

                var audioSubscription = _rtsp.AsAudioObservable()
                    .Subscribe(frame => echoCanceller.RegisterFramePlayed(frame.DecodedBytes.ToArray()));

                var subscription = Observable
                    .FromEventPattern<WaveInEventArgs>(h => waveIn.DataAvailable += h, h => waveIn.DataAvailable -= h)
                    .Select(x => x.EventArgs.Buffer)
                    .Do(echoCanceller.Write)
                    .Subscribe(_ =>
                    {
                        try
                        {
                            bool moreFrames;
                            do
                            {
                                var frameBuffer = new short[320];
                                if (echoCanceller.Read(frameBuffer, out moreFrames))
                                {
                                    _backChannelConnection.Send(frameBuffer);
                                }
                            } while (moreFrames);
                        }
                        catch
                        {
                            _activeCall.Dispose();
                        }
                    });

                waveIn.StartRecording();

                _rtsp.SetAudioState(false);

                _activeCall = Disposable.Create(() =>
                {
                    IsActive = false;

                    _rtsp.SetAudioState(true);

                    if (disconnect)
                    {
                        _backChannelConnection.Disconnect();
                    }

                    audioSubscription.Dispose();
                    subscription.Dispose();
                    waveIn.StopRecording();
                    waveIn.Dispose();
                });
            }
            else
            {
                _activeCall?.Dispose();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _activeCall?.Dispose();
            }
        }
    }
}
