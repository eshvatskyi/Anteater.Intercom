using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Anteater.Intercom.Device.Events;
using Anteater.Intercom.Device.Rtsp;
using Anteater.Intercom.Gui.Views;

namespace Anteater.Intercom
{
    sealed partial class App : Application
    {
        private const string UniqueEventName = "{D2F9052F-CD44-44B7-8394-C91D8B0708F1}";

        private const string UniqueMutexName = "{DCF63EF4-F686-47C0-B5C8-36F94F35FE73}";

        private EventWaitHandle _eventWaitHandle;

        private Mutex _mutex;

        public App()
        {
            AlarmEvents = new AlarmEventsService();
            Rtsp = new RtspDataService();
        }

        public AlarmEventsService AlarmEvents { get; }

        public RtspDataService Rtsp { get; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _mutex = new Mutex(true, UniqueMutexName, out var isOwned);
            _eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, UniqueEventName);

            GC.KeepAlive(_mutex);

            if (isOwned)
            {
                new Thread(() =>
                {
                    while (_eventWaitHandle.WaitOne())
                    {
                        Current.Dispatcher.Invoke(() => ((MainWindow)Current.MainWindow).BringToForeground(), DispatcherPriority.Send);
                    }
                })
                {
                    IsBackground = true
                }.Start();

                AlarmEvents.Start();
                Rtsp.Start();

                return;
            }

            _eventWaitHandle.Set();

            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            AlarmEvents.Stop();
            Rtsp.Stop();

            base.OnExit(e);
        }
    }
}
