using System;
using System.Net.Sockets;
using System.Windows;
using OpenRm.Agent.CustomControls;
using OpenRm.Common.Entities;
using System.Configuration;

namespace OpenRm.Agent
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static bool agentStarted = false;            // flag that indicates current status of agent. can be changed by pressing "Stop Agent" control
        private string serverIP;
        private int serverPort;
        private string logFilenamePattern;
        public static int ReceiveBufferSize = 64;      //recieve buffer size for tcp connection


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
            if (ReadConfigFile())
            {

                Logger.CreateLogFile("logs", logFilenamePattern);       // creates "logs" directory in binaries folder and set log filename
                Logger.WriteStr("Client started.");
                agentStarted = true;

                TCPclient client = new TCPclient(serverIP, serverPort, ReceiveBufferSize);

                Logger.WriteStr("Client terminated");
            }

        }


        // read configuration from config file
        private bool ReadConfigFile()
        {
            try
            {
                 
                serverIP = ConfigurationManager.AppSettings["ServerIP"];
                serverPort = Int32.Parse(ConfigurationManager.AppSettings["ServerPort"]);
                logFilenamePattern = ConfigurationManager.AppSettings["LogFilePattern"];
            }
            catch (Exception ex)
            {
                Logger.CriticalToEventLog("Error while reading config file. Error: " + ex.Message);
                return false;
            }

            return true;   
        }


        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            _notifyIconComponent.Dispose();
        }
    }
}
