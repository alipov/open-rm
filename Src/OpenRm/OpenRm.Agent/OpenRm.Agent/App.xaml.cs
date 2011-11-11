using System;
using System.Threading;
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
        private static Thread starterThread;
        private TcpClient client;

        private NotifyIconWrapper _notifyIconComponent;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Done because exception was thrown before Main. Solution found here:
            // http://www.codeproject.com/Questions/184743/AssemblyResolve-event-not-hit
            TypeResolving.RegisterTypeResolving();
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            _notifyIconComponent = new NotifyIconWrapper();
            _notifyIconComponent.StartAgentClick += StartThread;
        }


        private void StartThread(object sender, EventArgs e)
        {
            starterThread = new Thread(AgentStarterThread);
            starterThread.Start();
        }

        private void AgentStarterThread()
        {
            if (!ReadConfigFile()) return; //TODO: close application? notify user?

            Logger.CreateLogFile("logs", logFilenamePattern);       // creates "logs" directory in binaries folder and set log filename
            Logger.WriteStr("Client started.");
            agentStarted = true;
            
            client = new TcpClient(serverIP, serverPort, ReceiveBufferSize, TypeResolving.AssemblyResolveHandler);
            client.Start();

            Logger.WriteStr("Client terminated");

        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            if (starterThread != null)
            {
                //TODO: close socket, close client
                //...
                agentStarted = false;
                starterThread.Abort();
                starterThread = null;
            }

            _notifyIconComponent.Dispose();
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
    }
}
