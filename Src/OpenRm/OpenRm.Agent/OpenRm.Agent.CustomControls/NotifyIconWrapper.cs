using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using OpenRm.Agent.CustomControls.Views;
using Application = System.Windows.Application;

namespace OpenRm.Agent.CustomControls
{
    public partial class NotifyIconWrapper : Component
    {
        // Use just one instance of this window.
        public SettingsView _settingsView;
        private Icon _greenIcon;
        private Icon _redIcon;

        

        public NotifyIconWrapper()
        {
            InitializeComponent();

            InitializeResources();
        }

        public NotifyIconWrapper(IContainer container)
        {
            container.Add(this);

            InitializeComponent();

            InitializeResources();
        }

        private void InitializeResources()
        {
            startAgentMenuItem.Click += StartAgentMenuItemClickEventHandler;
            stopAgentMenuItem.Click += StopAgentMenuItemClickEventHandler;
            settingsMenuItem.Click += SettingsMenuItemClickEventHandler;
            quitMenuItem.Click += QuitMenuItemClickEventHandler;

            _greenIcon = LoadIcon
                ("/OpenRm.Agent.CustomControls;component/Images/GreenIcon.ico", "Failed to load Images/GreenIcon.ico");
            _redIcon = LoadIcon
                ("/OpenRm.Agent.CustomControls;component/Images/RedIcon.ico", "Failed to load Images/RedIcon.ico");

            OpenRmNotifyIcon.Icon = _redIcon;
        }

        private Icon LoadIcon(string uriPath, string messageOnFail)
        {
            Icon result;
            var iconUri = new Uri(uriPath, UriKind.RelativeOrAbsolute);
            var resourceStream = Application.GetResourceStream(iconUri);
            if (resourceStream == null)
                throw new ApplicationException(messageOnFail);

            using (Stream iconStream = resourceStream.Stream)
            {
                result = new Icon(iconStream);
            }

            return result;
        }

        private EventHandler _startAgentClick;
        private EventHandler _stopAgentClick;
        private EventHandler _settingsAgentChanged;

        public event EventHandler StartAgentClick
        {
            add { _startAgentClick += value; }
            remove { _startAgentClick -= value; }
        }

        public event EventHandler StopAgentClick
        {
            add { _stopAgentClick += value; }
            remove { _stopAgentClick -= value; }
        }

        public event EventHandler SettingsAgentChanged
        {
            add { _settingsAgentChanged += value; }
            remove { _settingsAgentChanged -= value; }
        }

        public void ShowNotifiction(string text)
        {
            OpenRmNotifyIcon.ShowBalloonTip(10000, "OpenRM Agent", text, ToolTipIcon.Info);
        }


        private void RestartAgent(object sender, EventArgs e)
        {
            _settingsAgentChanged.Invoke(sender, e);
            StopAgentMenuItemClickEventHandler(sender, e);
            StartAgentMenuItemClickEventHandler(sender, e);
        }

        private void StartAgentMenuItemClickEventHandler(object sender, EventArgs e)
        {
            startAgentMenuItem.Enabled = false;
            stopAgentMenuItem.Enabled = true;
            OpenRmNotifyIcon.Icon = _greenIcon;

            _startAgentClick.Invoke(sender, e);
        }

        private void StopAgentMenuItemClickEventHandler(object sender, EventArgs e)
        {
            stopAgentMenuItem.Enabled = false;
            startAgentMenuItem.Enabled = true;
            OpenRmNotifyIcon.Icon = _redIcon;

            _stopAgentClick.Invoke(sender, e);
        }

        private void SettingsMenuItemClickEventHandler(object sender, EventArgs e)
        {
            if(_settingsView == null)
                _settingsView = new SettingsView();

            _settingsView.ApplySettings += RestartAgent;

            // Show the window (and bring it to the forefront if it's already visible).
            if (_settingsView.WindowState == WindowState.Minimized)
                _settingsView.WindowState = WindowState.Normal;
            _settingsView.Show();
            _settingsView.Activate();
        }

        private void QuitMenuItemClickEventHandler(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
