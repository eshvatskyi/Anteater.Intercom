using System;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Anteater.Intercom.Device.Events;
using NAudio.Wave;

namespace Anteater.Intercom.Gui.ViewModel
{
    public class AlarmRingerViewModel : BaseViewModel
    {
        private readonly IWavePlayer _wavePlayer;

        private bool _isActive;
        private bool _isSoundMuted;

        public AlarmRingerViewModel(AlarmEventsService alarmEvents)
        {
            _wavePlayer = new WaveOut();

            var streamInfo = Application.GetResourceStream(new Uri("pack://application:,,,/Assets/DoorBell.mp3"));

            _wavePlayer.Init(new LoopStream(new Mp3FileReader(streamInfo.Stream)));

            ChangeActiveStateCommand = new RelayCommand<bool>(ChangeActiveState);

            ChangeMutedStateCommand = new RelayCommand(ChangeMuteState);

            alarmEvents.AsObservable()
                .Where(x => x.Type == AlarmEvent.EventType.SensorAlarm && x.Status)                
                .Select(x => true)
                .ObserveOnDispatcher(DispatcherPriority.Send)
                .Subscribe(ChangeActiveState);
        }

        public ICommand ChangeActiveStateCommand { get; }

        public ICommand ChangeMutedStateCommand { get; }

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public bool IsSoundMuted
        {
            get => _isSoundMuted;
            set => SetProperty(ref _isSoundMuted, value);
        }

        void ChangeActiveState(bool state)
        {
            if (state)
            {
                IsActive = true;

                if (!IsSoundMuted)
                {
                    _wavePlayer.Play();
                }

                Observable
                    .Timer(TimeSpan.FromSeconds(15))
                    .ObserveOnDispatcher(DispatcherPriority.Send)
                    .Select(_ => false)
                    .Subscribe(ChangeActiveState);

            } else
            {
                IsActive = false;
                _wavePlayer.Stop();
            }
        }

        void ChangeMuteState()
        {
            IsSoundMuted = !IsSoundMuted;

            if (IsActive)
            {
                if (IsSoundMuted)
                {
                    _wavePlayer.Stop();
                }
                else
                {
                    _wavePlayer.Play();
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _wavePlayer?.Stop();
                _wavePlayer?.Dispose();
            }
        }

        class LoopStream : WaveStream
        {
            private readonly WaveStream _sourceStream;

            public LoopStream(WaveStream sourceStream)
            {
                _sourceStream = sourceStream;
                EnableLooping = true;
            }

            public bool EnableLooping { get; set; }

            public override WaveFormat WaveFormat => _sourceStream.WaveFormat;

            public override long Length => _sourceStream.Length;

            public override long Position
            {
                get => _sourceStream.Position;
                set => _sourceStream.Position = value;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                int totalBytesRead = 0;

                while (_sourceStream != null && totalBytesRead < count)
                {
                    int bytesRead = _sourceStream.Read(buffer, offset + totalBytesRead, count - totalBytesRead);
                    if (bytesRead == 0)
                    {
                        if (_sourceStream.Position == 0 || !EnableLooping)
                        {
                            break;
                        }

                        _sourceStream.Position = 0;
                    }

                    totalBytesRead += bytesRead;
                }

                return totalBytesRead;
            }

            protected override void Dispose(bool disposing)
            {
                _sourceStream?.Dispose();

                base.Dispose(disposing);
            }
        }
    }
}
