using System;
using System.Net;
using System.Threading;
using System.Windows;
using OpenRm.Agent.CustomControls;
using OpenRm.Common.Entities;
using OpenRm.Common.Entities.Network;
using OpenRm.Common.Entities.Network.Messages;

namespace OpenRm.Agent
{
    /// <summary>
    /// Interaction logic for Agent
    /// </summary>
    public partial class App
    {
        public static bool AgentStarted = false;            // flag that indicates current status of agent. can be changed by pressing "Stop Agent" control
        private IPEndPoint _newServerEndPoint = null;          //got from GUI
        private static Thread _starterThread;
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
            _notifyIconComponent.SettingsAgentChanged += UpdateServerIpEndpoint;
        }

        // called from Notification Icon
        private void StartAgentThread(object sender, EventArgs e)
        {
            _starterThread = new Thread(AgentStarterThread);
            _starterThread.Start();
        }

        private void AgentStarterThread()
        {
            // Read configuration from file
            if (!SettingsManager.ReadConfigFile()) return;  //TODO: close application? notify user?

            if (_newServerEndPoint != null)
                SettingsManager.ServerEndPoint = _newServerEndPoint;       //override configuration file

            EncryptionAdapter.SetEncryption(SettingsManager.SecretKey);

            Logger.CreateLogFile("logs", SettingsManager.LogFilenamePattern);       // creates "logs" directory in binaries folder and set log filename
            Logger.WriteStr("+++ Starting Agent by user request... +++");

            AgentStarted = true;
            _retryIntervalCurrent = RetryIntervalInitial;            //set to initial value  

            while (true)  // always reconnect untill canceled
            {
                _client = new GeneralSocketClient();
                _client.Connect(SettingsManager.ServerEndPoint, OnConnectToServerCompleted);

                // pause here untill disconnected from server
                _clientDisconnected.Reset();
                _clientDisconnected.WaitOne();
                

                if (AgentStarted)
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
            if (_starterThread != null)
            {
                AgentStarted = false;
                if (_client.IsConnected)
                {
                    _client.Disconnect(null);
                }
                _starterThread.Abort();
                _starterThread = null;
            }
        }


        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            CloseAgent();

            _notifyIconComponent.Dispose();
        }


        private void UpdateServerIpEndpoint(object sender, EventArgs e)
        {
            if (e is AgentEventArgs)
            {
                _newServerEndPoint = ((AgentEventArgs) e).ServerEP;
            }

        }

        
    }
}
