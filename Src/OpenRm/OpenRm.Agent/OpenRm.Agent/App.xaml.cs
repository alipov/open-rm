using System;
using System.Net;
using System.Threading;
using System.Windows;
using OpenRm.Agent.CustomControls;
using OpenRm.Common.Entities;
using System.Configuration;
using OpenRm.Common.Entities.Network;
using OpenRm.Common.Entities.Network.Messages;

namespace OpenRm.Agent
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static bool agentStarted = false;            // flag that indicates current status of agent. can be changed by pressing "Stop Agent" control
        private IPEndPoint _endPoint;
        private string _logFilenamePattern;
        //public static int ReceiveBufferSize = 64;      //recieve buffer size for tcp connection
        private static Thread starterThread;
        //private TcpClient _client;
        private IMessageClient _client;
        private IPEndPoint _localEndPoint;

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

            Logger.CreateLogFile("logs", _logFilenamePattern);       // creates "logs" directory in binaries folder and set log filename
            Logger.WriteStr("Client started.");
            agentStarted = true;

            //_client = new TcpClient();
            //_client.Connect(_endPoint, OnConnectToServerCompleted);
            _client = new GeneralSocketClient();
            _client.Connect(_endPoint, OnConnectToServerCompleted);

            Logger.WriteStr("Client terminated");

        }

        private void OnConnectToServerCompleted(CustomEventArgs args)
        {
            var idRequest = new IdentificationDataRequest();
            _localEndPoint = args.LocalEndPoint;
            var message = new ResponseMessage
                          {
                              Response = idRequest.ExecuteRequest()
                          };
            _client.Send(message, OnReceivingCompleted);

        }

        private void OnReceivingCompleted(CustomEventArgs args)
        {
            var requestMessage = args.Result as RequestMessage;
            if (requestMessage == null)
            {
                throw new Exception();
            }

            var request = requestMessage.Request;

            if (request is IpConfigRequest)
            {
                ((IpConfigRequest) request).IpAddress = _localEndPoint.Address;
            }

            var response = request.ExecuteRequest();
            var message = new ResponseMessage { Response = response };
            _client.Send(message, OnReceivingCompleted);
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
                var ip = ConfigurationManager.AppSettings["ServerIP"];
                var port = Int32.Parse(ConfigurationManager.AppSettings["ServerPort"]);
                _endPoint = new IPEndPoint(IPAddress.Parse(ip), port);
                _logFilenamePattern = ConfigurationManager.AppSettings["LogFilePattern"];
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
