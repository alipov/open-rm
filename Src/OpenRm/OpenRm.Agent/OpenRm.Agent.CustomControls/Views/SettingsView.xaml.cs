using System;
using System.ComponentModel;
using System.Net;
using System.Windows;
using System.Windows.Media;

namespace OpenRm.Agent.CustomControls.Views
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView : Window
    {
        public EventHandler _applySettings;

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

        public event EventHandler ApplySettings
        {
            add { _applySettings += value; }
            remove { _applySettings -= value; }
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            IPAddress newServerIp = null;
            try
            {
                newServerIp = IPAddress.Parse(ServerText.Text.Trim());
            }
            catch (Exception)
            {
                ServerText.Background = Brushes.Red;
            
            }

            int newPort = 0;
            try
            {
                newPort = Int32.Parse(PortText.Text.Trim());
                if (newPort < 1024 || newPort > 65535)
                    newPort = 0;
            }
            catch (Exception)
            {
                PortText.Background = Brushes.Red;
            }
            
            if (newServerIp != null && newPort != 0)
            {
                IPEndPoint ipEP = new IPEndPoint(newServerIp, newPort);
                var args = new AgentEventArgs(ipEP);
                _applySettings.Invoke(sender, args);
                WindowState = WindowState.Minimized;
                ShowInTaskbar = false;
  
            }

            
        }

        
    }
}
