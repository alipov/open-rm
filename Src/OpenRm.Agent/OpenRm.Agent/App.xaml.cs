using System;
using System.Net.Sockets;
using System.Windows;
using OpenRm.Agent.CustomControls;

namespace OpenRm.Agent
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private NotifyIconWrapper _notifyIconComponent;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            _notifyIconComponent = new NotifyIconWrapper();
            _notifyIconComponent.StartAgentClick += DoSomething;
        }

        private void DoSomething(object sender, EventArgs e)
        {

            var tcpClient = new TcpClient();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            _notifyIconComponent.Dispose();
        }
    }
}
