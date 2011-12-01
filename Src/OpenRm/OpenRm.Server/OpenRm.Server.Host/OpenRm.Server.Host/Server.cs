using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using OpenRm.Common.Entities;
using System.Configuration;
using OpenRm.Common.Entities.Network;
using OpenRm.Common.Entities.Network.Messages;
using OpenRm.Common.Entities.Network.Server;

namespace OpenRm.Server.Host
{
    internal class Server
    {
        // these variables will be read from app.config
        private int _listenPort;
        private int _maxNumConnections;     //maximum number of connections
        private string _logFilenamePattern;

        private HostAsyncUserToken _console;
        private Dictionary<int, HostAsyncUserToken> _agents;
        private int _agentsCount;
        private IMessageServer _server;

        private const int ReceiveBufferSize = 64;       //recieve buffer size for tcp connection
        
        public void Run()
        {
            if (ReadConfigFile())
            {
                Logger.CreateLogFile("logs", _logFilenamePattern);       // creates "logs" directory in binaries folder and set log filename
                Logger.WriteStr("Started");

                _agents = new Dictionary<int, HostAsyncUserToken>();

                _server = new TcpServerListenerAdv(_listenPort, _maxNumConnections, ReceiveBufferSize);
                //var srv = new TcpServerListener(_listenPort, _maxNumConnections, ReceiveBufferSize, TypeResolving.AssemblyResolveHandler);
                _server.Start(OnReceiveCompleted);

                // Pause here 
                Console.WriteLine("Press any key to terminate the server process....");
                Console.ReadKey();
                Logger.WriteStr("TCP terminated.");
            }
        }

        private void OnReceiveCompleted(HostCustomEventArgs args)
        {
            if (args.Result is RequestMessage)
                ProcessReceivedMessageRequest(args);
            else if (args.Result is ResponseMessage)
                ProcessReceivedMessageResponse(args);
            else
                throw new ArgumentException("Cannot determinate Message type!");
        }

        private void ProcessReceivedMessageRequest(HostCustomEventArgs args)
        {
            //server recieves requests only from Console!
            var message = (RequestMessage) args.Result;

            if (_console == null)
                _console = args.Token;

            if (message.Request is ListAgentsRequest)
            {
                var agentsResponse = new ListAgentsResponse()
                {
                    Agents = new List<Agent>()
                };

                foreach (var agent in _agents)
                {
                    var thisAgent = new Agent()
                        {
                            ID = agent.Key,
                            Name = agent.Value.ClientData.deviceName
                        };

                    agentsResponse.Agents.Add(thisAgent);
                }

                var responseMessage = new ResponseMessage()
                {
                    Response = agentsResponse
                };
                _server.Send(responseMessage, args.Token);
            }
        }


        private void ProcessReceivedMessageResponse(HostCustomEventArgs args)
        {
            var message = (ResponseMessage) args.Result;

            if (message.Response is IdentificationDataResponse)
            {
                var idata = (IdentificationDataResponse)message.Response;
                Logger.WriteStr(" * New client has connected: " + idata.deviceName);
                // ...create ClientData (if does not exist already) and add to token
                //...
                args.Token.ClientData = idata;
                if (_agents.All(a => a.Value.ClientData.deviceName != idata.deviceName))
                {
                    _agents.Add(Interlocked.Increment(ref _agentsCount), args.Token);
                }


                //TODO: for testing only:
                //Get IP information
                //var msg = new RequestMessage { OpCode = (int)EOpCode.IpConfigData };

                var msg = new RequestMessage { OpCode = (int)EOpCode.RunProcess };
                var exec = new RunProcessRequest
                {
                    RunId = HostAsyncUserToken.RunId,
                    Cmd = "notepad.exe",
                    Args = "",
                    WorkDir = "c:\\",
                    TimeOut = 180000,        //ms
                    Hidden = true
                };
                msg.Request = exec;
                _server.Send(msg, args.Token);
            }
            else if (message.Response is IpConfigResponse)
            {
                var ipConf = (IpConfigResponse)message.Response;
                args.Token.agentData.IpConfig = ipConf;       //store in "database"


                //TODO: move to another place
                var msg = new RequestMessage { OpCode = (int)EOpCode.RunProcess };
                var exec = new RunProcessRequest
                {
                    RunId = HostAsyncUserToken.RunId,
                    Cmd = "notepad.exe",
                    Args = "",
                    WorkDir = "c:\\",
                    TimeOut = 180000,        //ms
                    Hidden = true
                };

                msg.Request = exec;
                _server.Send(msg, args.Token);
            }
            else if (message.Response is RunProcessResponse)
            {
                var status = (RunProcessResponse)message.Response;
                if (status.ExitCode == 0)
                {
                    Logger.WriteStr("Remote successfully executed");
                }
                else if (status.ExitCode > 0)
                {
                    Logger.WriteStr("Remote program executed with exit code: " + status.ExitCode +
                                    "and error message: \"" + status.ErrorMessage + "\"");
                }
                else
                {
                    throw new ArgumentException("Invalid exit code of remote execution (" + status.ExitCode + ")");
                }


                //TODO: for testing only:
                var msg = new RequestMessage { OpCode = (int)EOpCode.InstalledPrograms };
                _server.Send(msg, args.Token);

            }
            else if (message.Response is InstalledProgramsResponse)
            {
                var progsList = (InstalledProgramsResponse)message.Response;
                foreach (string s in progsList.Progs)
                {
                    Console.WriteLine(s);
                }



                //}
                //else if (message.Response is )
                //{
                //............................
                //
                //...
            }
            else
            {
                Logger.WriteStr(string.Format("WARNING: Recieved unkown response from {0}!", 
                                                                args.Token.Socket.RemoteEndPoint));
            }

        }


        // read configuration from config file 
        private bool ReadConfigFile()
        {
            try {
                _listenPort = Int32.Parse(ConfigurationManager.AppSettings["ListenOnPort"]);
                _maxNumConnections = Int32.Parse(ConfigurationManager.AppSettings["MaxConnections"]);
                _logFilenamePattern = ConfigurationManager.AppSettings["LogFilePattern"];
            }
            catch (Exception ex) 
            {
                Logger.CriticalToEventLog("Error while reading config file: \n " + ex.Message);
                return false;
            }

            return true;   
        }
    }
}
