using System.Windows;
using System.Windows.Threading;
using Anteater.Intercom.Gui.ViewModel;

namespace Anteater.Intercom.Gui.Views
{
    sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            var app = Application.Current as App;

            DataContext = app != null ? new MainWindowViewModel(app.AlarmEvents) : null;

            InitializeComponent();

            Dispatcher.ShutdownStarted += (s, e) => (DataContext as MainWindowViewModel)?.Dispose();
        }

        public void BringToForeground()
        {
            Show();
            Activate();
            Focus();
            (DataContext as MainWindowViewModel).IsMaximized = true;
        }
    }
}
