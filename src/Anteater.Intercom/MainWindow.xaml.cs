using System;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using static PInvoke.User32;

namespace Anteater.Intercom
{
    sealed partial class MainWindow : Window
    {
        private const int WmSyscommand = 0x0112;
        private const int ScMonitorpower = 0xF170;
        private const int MonitorShutoff = 2;
        private const int MonitorOnPtr = -1;

        public static MainWindow Instance { get; private set; }

        private readonly IntPtr _hwnd;
        private readonly AppWindow _appWindow;

        private bool _isFullScreen = false;

        public MainWindow()
        {
            Instance = this;

            Title = "Home Guard";

            InitializeComponent();

            _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

            var windowId = Win32Interop.GetWindowIdFromWindow(_hwnd);

            _appWindow = AppWindow.GetFromWindowId(windowId);
            _appWindow.Show(true);
            _appWindow.SetIcon("Assets/Icon.ico");

            InitialSizeAndPosition();

            DispatcherQueue.TryEnqueue(delegate
            {
                NavigateToType(typeof(Gui.Pages.Intercom));
            });
        }

        public bool FullScreenMode
        {
            get => _isFullScreen;
            set
            {
                if (value)
                {
                    _appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
                }
                else
                {
                    _appWindow.SetPresenter(AppWindowPresenterKind.Overlapped);

                    InitialSizeAndPosition();
                }

                _isFullScreen = value;
            }
        }

        public void BringToForeground()
        {
            DispatcherQueue.TryEnqueue(delegate
            {
                PInvoke.User32.SetForegroundWindow(_hwnd);

                FullScreenMode = true;
            });
        }

        public void NavigateToType(Type type)
        {
            MainFrame.NavigateToType(type, null, null);
        }

        public void MonitorOff()
        {
            SendMessage(_hwnd, (WindowMessage)WmSyscommand, (IntPtr)ScMonitorpower, (IntPtr)MonitorShutoff);
        }

        unsafe public void MonitorOn()
        {
            SendMessage(_hwnd, (WindowMessage)WmSyscommand, (IntPtr)ScMonitorpower, (IntPtr)MonitorOnPtr);
            //var mouseMove = new INPUT();

            //mouseMove.type = InputType.INPUT_MOUSE;
            //mouseMove.Inputs.mi.dwFlags = MOUSEEVENTF.MOUSEEVENTF_LEFTDOWN;

            //SendInput(1, &mouseMove, Marshal.SizeOf(new INPUT()));
        }

        void InitialSizeAndPosition()
        {
            var display = DisplayArea.GetFromWindowId(_appWindow.Id, DisplayAreaFallback.Primary);

            _appWindow.Resize(new() { Width = 800, Height = 600 });
            _appWindow.Move(new() { X = (display.WorkArea.Width - 800) / 2, Y = (display.WorkArea.Height - 600) / 2 });
        }
    }
}
