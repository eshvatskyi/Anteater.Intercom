using System;
using System.Threading;
using System.Threading.Tasks;
using Anteater.Intercom.Device.Events;
using Anteater.Pipe;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Anteater.Intercom.Gui.Pages
{
    sealed partial class Intercom : Page
    {
        public static readonly DependencyProperty IsOverlayVisibleProperty = DependencyProperty
            .Register(nameof(IsOverlayVisible), typeof(bool), typeof(Intercom), PropertyMetadata
            .Create(false));

        private readonly IPipe _pipe;

        private Task _commandTask = Task.CompletedTask;
        private CancellationTokenSource _cts;
        private bool _overlayLocked = false;

        public Intercom()
        {
            _pipe = App.ServiceProvider.GetRequiredService<IPipe>();

            var alarmEvents = _pipe.Subscribe<AlarmEvent>(x => x.Where(x => x.Status && x.Type switch
            {
                AlarmEvent.EventType.MotionDetection => true,
                AlarmEvent.EventType.SensorAlarm => true,
                _ => false
            }).Do(x => DispatcherQueue.TryEnqueue(delegate
            {
                IsOverlayVisible = true;
                ApplyOverlayChanges();
            })));

            var callStateChanged = _pipe.Subscribe<CallStateChanged>(x =>
            {
                if (_overlayLocked = x.IsCalling)
                {
                    _cts?.Cancel();
                }
                else
                {
                    DispatcherQueue.TryEnqueue(ApplyOverlayChanges);
                }

                return Task.CompletedTask;
            });

            InitializeComponent();

            IsOverlayVisible = true;
            ApplyOverlayChanges();

            void UnloadEventHandler()
            {
                _cts?.Cancel();
                _commandTask = Task.CompletedTask;
                alarmEvents.Dispose();
                callStateChanged.Dispose();
            };

            Unloaded += (_, _) => UnloadEventHandler();
            MainWindow.Instance.Closed += (_, _) => UnloadEventHandler();
        }

        public bool IsOverlayVisible
        {
            get => Convert.ToBoolean(GetValue(IsOverlayVisibleProperty));
            set => SetValue(IsOverlayVisibleProperty, value);
        }

        protected override void OnTapped(TappedRoutedEventArgs e)
        {
            e.Handled = true;
            IsOverlayVisible = true;
            ApplyOverlayChanges();
        }

        protected override void OnDoubleTapped(DoubleTappedRoutedEventArgs e)
        {
            e.Handled = true;
            MainWindow.Instance.FullScreenMode = !MainWindow.Instance.FullScreenMode;
        }

        void ApplyOverlayChanges()
        {
            if (_overlayLocked)
            {
                return;
            }

            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            if (IsOverlayVisible)
            {
                _commandTask = _commandTask.ContinueWith(async () =>
                    await _pipe.ExecuteAsync(new ChangeVideoState(false)));

                _ = Task.Delay(TimeSpan.FromSeconds(15), _cts.Token).ContinueWith(_ =>
                {
                    _cts = null;
                    DispatcherQueue.TryEnqueue(delegate
                    {
                        IsOverlayVisible = false;
                        ApplyOverlayChanges();
                    });
                }, TaskContinuationOptions.OnlyOnRanToCompletion);
            }
            else
            {
                _ = Task.Delay(TimeSpan.FromSeconds(30), _cts.Token).ContinueWith(_ =>
                {
                    _cts = null;
                    _commandTask = _commandTask.ContinueWith(async () =>
                        await _pipe.ExecuteAsync(new ChangeVideoState(true)));
                }, TaskContinuationOptions.OnlyOnRanToCompletion);
            }
        }

        private void Button_Tapped(object sender, TappedRoutedEventArgs e)
        {
            MainWindow.Instance.NavigateToType(typeof(Settings));
        }
    }
}
