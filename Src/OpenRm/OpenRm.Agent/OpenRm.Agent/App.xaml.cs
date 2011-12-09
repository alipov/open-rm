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
    /// Interaction logic for Agent
    /// </summary>
    public partial class App : Application
    {
        public static bool agentStarted = false;            // flag that indicates current status of agent. can be changed by pressing "Stop Agent" control
        private IPEndPoint _endPoint;
        private string _logFilenamePattern;
        private static Thread starterThread;
        private IMessageClient _client;
        private IPEndPoint _localEndPoint;
        private NotifyIconWrapper _notifyIconComponent;

        // Intervals between client reconnects (when was unable to connect / has been disconnected)    
        private int _retryIntervalCurrent;
        private const int RetryIntervalInitial = 5; // 5 sec.
        private const int RetryIntervalMaximum = 60; // 60 sec.

        // pauses thread untill client disconnected
        private readonly ManualResetEvent _clientDisconnected = new ManualResetEvent(false);  

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Done because exception was thrown before Main. Solution found here:
            // http://www.codeproject.com/Questions/184743/AssemblyResolve-event-not-hit
            TypeResolving.RegisterTypeResolving();
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            _notifyIconComponent = new NotifyIconWrapper();
            _notifyIconComponent.StartAgentClick += StartAgentThread;
            _notifyIconComponent.StopAgentClick += StopAgentThread;
        }

        // called from Notification Icon
        private void StartAgentThread(object sender, EventArgs e)
        {
            starterThread = new Thread(AgentStarterThread);
            starterThread.Start();
        }

        private void AgentStarterThread()
        {
            if (!ReadConfigFile()) return; //TODO: close application? notify user?

            Logger.CreateLogFile("logs", _logFilenamePattern);       // creates "logs" directory in binaries folder and set log filename
            Logger.WriteStr("+++ Starting Agent by user request... +++");

            agentStarted = true;
            _retryIntervalCurrent = RetryIntervalInitial;            //set to initial value  

            while (true)  // always reconnect untill canceled
            {
                _client = new GeneralSocketClient();
                _client.Connect(_endPoint, OnConnectToServerCompleted);

                // pause here untill disconnected from server
                _clientDisconnected.Reset();
                _clientDisconnected.WaitOne();
                

                if (agentStarted)
                {
                    Logger.WriteStr("Should be running so will try to reconnect to server in " + _retryIntervalCurrent + "seconds...");
                    _notifyIconComponent.ShowNotifiction("Will reconnect in " + _retryIntervalCurrent + "sec...");
                    Thread.Sleep(_retryIntervalCurrent * 1000);

                    //increase interval between reconnects up to retryIntervalMaximum value.
                    // it needed in case of frequent disconnects / when server is unreacheble
                    if (_retryIntervalCurrent < RetryIntervalMaximum)
                        _retryIntervalCurrent += 5;
                }
                else
                {
                    break;  //exit loop
                }
                
            }

            Logger.WriteStr("Client terminated");
        }


        private void OnConnectToServerCompleted(CustomEventArgs args)
        {
            if (args.LocalEndPoint == null)
            {
                _notifyIconComponent.ShowNotifiction("Cannot connect to server. Will retry in " + _retryIntervalCurrent + "sec...");
                _clientDisconnected.Set();
                return;
            }

            _notifyIconComponent.ShowNotifiction("Successfully connected to server");

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
            var requestMessage = (RequestMessage)args.Result;

            //returned "null" means connection has been closed
            if (requestMessage == null)
            {
                _notifyIconComponent.ShowNotifiction("Disconnected from server.");
                _clientDisconnected.Set();
                return;
            }

            var request = requestMessage.Request;

            if (request is IpConfigRequest)
            {
                ((IpConfigRequest) request).IpAddress = _localEndPoint.Address;
            }

            var response = request.ExecuteRequest();
            var message = new ResponseMessage(requestMessage.UniqueID)
                              {
                                  Response = response
                              };
            _client.Send(message, OnReceivingCompleted);
        }


        // called from Notification Icon
        private void StopAgentThread(object sender, EventArgs e)
        {
            Logger.WriteStr("--- Stopping Agent by user request... ---");
            _notifyIconComponent.ShowNotifiction("Still processing previous requests... Will close shortly...");
            CloseAgent();
        }

        private void CloseAgent()
        {
            if (starterThread != null)
            {
                agentStarted = false;
                if (_client.IsConnected)
                {
                    _client.Disconnect(null);
                }
                starterThread.Abort();
                starterThread = null;
            }
        }


        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            CloseAgent();

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
