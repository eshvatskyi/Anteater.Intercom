using System;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using static PInvoke.User32;

namespace Anteater.Intercom;

sealed partial class MainWindow : Window
{
    public static MainWindow Instance { get; private set; }

    private readonly IntPtr _hwnd;
    private readonly AppWindow _appWindow;

    private bool _isFullScreen = false;

    public MainWindow()
    {
        Instance = this;

        Title = "Home Guard";

        _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

        var windowId = Win32Interop.GetWindowIdFromWindow(_hwnd);

        _appWindow = AppWindow.GetFromWindowId(windowId);
        _appWindow.Show(true);
        _appWindow.SetIcon("Assets/Icon.ico");

        InitialSizeAndPosition();

        InitializeComponent();

        Activate();

        BringToForeground();

        NavigateToType(typeof(Gui.Pages.Intercom));
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

    void InitialSizeAndPosition()
    {
        var display = DisplayArea.GetFromWindowId(_appWindow.Id, DisplayAreaFallback.Primary);

        _appWindow.Resize(new() { Width = 800, Height = 600 });
        _appWindow.Move(new() { X = (display.WorkArea.Width - 800) / 2, Y = (display.WorkArea.Height - 600) / 2 });
    }

    public void BringToForeground()
    {
        DispatcherQueue.TryEnqueue(delegate
        {
            SetForegroundWindow(_hwnd);

            FullScreenMode = true;
        });
    }

    public void NavigateToType(Type type, object parameter = null)
    {
        MainFrame.NavigateToType(type, parameter, null);
    }
}
