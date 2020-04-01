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

            if (app != null)
            {
                ViewModel = new MainWindowViewModel(app.AlarmEvents);
            }

            DataContext = ViewModel;

            InitializeComponent();

            Dispatcher.ShutdownStarted += (s, e) => ViewModel?.Dispose();
        }

        public MainWindowViewModel ViewModel { get; }

        public void BringToForeground()
        {
            Show();
            Activate();
            Focus();
            ViewModel.IsMaximized = true;
        }
    }
}
