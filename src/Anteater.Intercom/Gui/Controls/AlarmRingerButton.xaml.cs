using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Anteater.Intercom.Device.Events;
using Anteater.Pipe;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using NAudio.Wave;

namespace Anteater.Intercom.Gui.Controls
{
    public partial class AlarmRingerButton : Button
    {
        public static readonly DependencyProperty IsActiveProperty = DependencyProperty
            .Register(nameof(IsActive), typeof(bool), typeof(AlarmRingerButton), PropertyMetadata
            .Create(false, (o, _) => (o as AlarmRingerButton)?.OnIsActiveChanged()));

        private readonly IEventPublisher _pipe;
        private readonly WaveOut _soundPlayer;

        private CancellationTokenSource _cts;

        public AlarmRingerButton()
        {
            _pipe = App.ServiceProvider.GetRequiredService<IEventPublisher>();

            var alarmEvent = _pipe.Subscribe<AlarmEvent>(x => x
                .Where(x => x.Status && x.Type == AlarmEvent.EventType.SensorAlarm)
                .Do(x => DispatcherQueue.TryEnqueue(delegate { IsActive = true; })));

            var alarmStateChanged = _pipe.Subscribe<AlarmStateChanged>(x =>
            {
                _soundPlayer.Volume = x.IsMuted ? 0 : 1;

                return Task.CompletedTask;
            });

            var callStateChanged = _pipe.Subscribe<CallStateChanged>(x =>
            {
                DispatcherQueue.TryEnqueue(delegate { IsActive = false; });
                return Task.CompletedTask;
            });

            var doorLockStateChanged = _pipe.Subscribe<DoorLockStateChanged>(x =>
            {
                DispatcherQueue.TryEnqueue(delegate { IsActive = false; });
                return Task.CompletedTask;
            });

            _soundPlayer = new WaveOut();

            var stream = File.OpenRead("Assets/DoorBell.mp3");

            _soundPlayer.Init(new WaveLoopStream(new Mp3FileReader(stream)));

            InitializeComponent();

            IsActive = false;

            void UnloadEventHandler()
            {
                _cts?.Cancel();
                alarmEvent.Dispose();
                alarmStateChanged.Dispose();
                callStateChanged.Dispose();
                doorLockStateChanged.Dispose();
                _soundPlayer.Stop();
                _soundPlayer?.Dispose();
            };

            Unloaded += (_, _) => UnloadEventHandler();
            MainWindow.Instance.Closed += (_, _) => UnloadEventHandler();
        }

        public bool IsActive
        {
            get => Convert.ToBoolean(GetValue(IsActiveProperty));
            set => SetValue(IsActiveProperty, value);
        }

        void OnIsActiveChanged()
        {
            _cts?.Cancel();

            if (!IsActive)
            {
                _soundPlayer.Stop();

                return;
            }

            _cts = new CancellationTokenSource();

            _ = Task.Delay(TimeSpan.FromSeconds(15), _cts.Token).ContinueWith(_ =>
            {
                DispatcherQueue.TryEnqueue(delegate { IsActive = false; });
            });

            _soundPlayer.Play();
        }

        protected override void OnTapped(TappedRoutedEventArgs e)
        {
            e.Handled = false;

            IsActive = false;
        }
    }
}
