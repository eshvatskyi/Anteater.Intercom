using System.Windows;
using System.Windows.Controls;
using Anteater.Intercom.Gui.ViewModel;

namespace Anteater.Intercom.Gui.Views
{
    public partial class AlarmRinger : UserControl
    {
        public AlarmRinger()
        {
            var app = Application.Current as App;
            if (app != null)
            {
                ViewModel = new AlarmRingerViewModel(app.AlarmEvents);
            }

            DataContext = ViewModel;

            InitializeComponent();

            Dispatcher.ShutdownStarted += (s, e) => ViewModel?.Dispose();
        }

        public AlarmRingerViewModel ViewModel { get; }
    }
}
