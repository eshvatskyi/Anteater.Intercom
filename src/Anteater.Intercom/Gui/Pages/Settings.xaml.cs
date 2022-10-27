using Microsoft.UI.Xaml.Controls;

namespace Anteater.Intercom.Gui.Pages
{
    sealed partial class Settings : Page
    {
        public Settings()
        {
            InitializeComponent();
        }

        private void Button_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            MainWindow.Instance.NavigateToType(typeof(Intercom));
        }
    }
}
