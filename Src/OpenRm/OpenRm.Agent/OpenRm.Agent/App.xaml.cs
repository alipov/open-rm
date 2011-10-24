using System;
using System.Net.Sockets;
using System.Windows;
using OpenRm.Agent.CustomControls;
using OpenRm.Common.Entities;

namespace OpenRm.Agent
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        private const string LogFilenamePattern = "client-<date>.log";
        public static bool agentStarted = false;            // flag that indicates current status of agent. can be changed by pressing "Stop Agent" control

        private NotifyIconWrapper _notifyIconComponent;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            _notifyIconComponent = new NotifyIconWrapper();
            _notifyIconComponent.StartAgentClick += StartAgent;
        }

        private void StartAgent(object sender, EventArgs e)
        {
            Logger.CreateLogFile("logs", LogFilenamePattern);
            Logger.WriteStr("Client started.");
            agentStarted = true;

            // TODO: read configuration from config file 
            TCPclient client = new TCPclient("127.0.0.1", 3777, 64);

            Logger.WriteStr("Client terminated");

        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            _notifyIconComponent.Dispose();
        }
    }
}
