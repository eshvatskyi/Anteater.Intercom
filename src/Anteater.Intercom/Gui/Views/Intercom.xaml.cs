using System.Windows;
using System.Windows.Controls;
using Anteater.Intercom.Gui.ViewModel;

namespace Anteater.Intercom.Gui.Views
{
    public partial class Intercom : UserControl
    {
        public Intercom()
        {
            var app = Application.Current as App;

            if (app != null)
            {
                ViewModel = new IntercomViewModel(app.Rtsp, app.AlarmEvents);
            }

            DataContext = ViewModel;

            InitializeComponent();

            Dispatcher.ShutdownStarted += (s, e) => ViewModel?.Dispose();
        }

        public IntercomViewModel ViewModel { get; }
    }
}
