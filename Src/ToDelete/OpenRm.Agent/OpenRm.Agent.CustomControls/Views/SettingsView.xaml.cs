using System.ComponentModel;
using System.Windows;

namespace OpenRm.Agent.CustomControls.Views
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView : Window
    {
        public SettingsView()
        {
            InitializeComponent();
            Closing += WindowClosingEventHandler;
        }

        private void WindowClosingEventHandler(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            WindowState = WindowState.Minimized;
            ShowInTaskbar = false;
        }
    }
}
