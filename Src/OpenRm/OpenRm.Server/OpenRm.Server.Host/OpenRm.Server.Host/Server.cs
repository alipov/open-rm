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
        private static int _listenPort;
        private static int _maxNumConnections;     //maximum number of connections
        private static string _logFilenamePattern;

        private static HostAsyncUserToken _console;
        private static Dictionary<int, HostAsyncUserToken> _agents;
        private static int _agentsCount;
        private static TcpServerListenerAdv _server;

        private const int ReceiveBufferSize = 64; //recieve buffer size for tcp connection

        static void Main()
        {
            // Done because exception was thrown before Main. Solution found here:
            // http://www.codeproject.com/Questions/184743/AssemblyResolve-event-not-hit
            TypeResolving.RegisterTypeResolving();

            //Main code
            StartHost();
        }

        private static void StartHost()
        {
            if (ReadConfigFile())
            {
                Logger.CreateLogFile("logs", _logFilenamePattern);       // creates "logs" directory in binaries folder and set log filename
                Logger.WriteStr("Started");

                _agents = new Dictionary<int, HostAsyncUserToken>();

                _server = new TcpServerListenerAdv(_listenPort, _maxNumConnections, ReceiveBufferSize, TypeResolving.AssemblyResolveHandler);
                //var srv = new TcpServerListener(_listenPort, _maxNumConnections, ReceiveBufferSize, TypeResolving.AssemblyResolveHandler);
                _server.Start(ProcessReceivedMessage);

                // Pause here 
                Console.WriteLine("Press any key to terminate the server process....");
                Console.ReadKey();
                Logger.WriteStr("TCP terminated.");
            }
        }

        private static void ProcessReceivedMessage(HostCustomEventArgs args)
        {
            if (args.Result is RequestMessage)
                ProcessReceivedMessageRequest(args);
            else if (args.Result is ResponseMessage)
                ProcessReceivedMessageResponse(args);
            else
                throw new ArgumentException("Cannot determinate Message type!");
        }

        private static void ProcessReceivedMessageRequest(HostCustomEventArgs args)
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


        private static void ProcessReceivedMessageResponse(HostCustomEventArgs args)
        {
            var message = (ResponseMessage) args.Result;

            if (message.Response is IdentificationData)
            {
                var idata = (IdentificationData)message.Response;
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
                var msg = new RequestMessage { OpCode = (int)EOpCode.IpConfigData };
                msg = new RequestMessage { OpCode = (int)EOpCode.RunProcess };
                var exec = new RunProcess
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
            else if (message.Response is IpConfigData)
            {
                var ipConf = (IpConfigData)message.Response;
                args.Token.agentData.IpConfig = ipConf;       //store in "database"


                //TODO: move to another place
                var msg = new RequestMessage { OpCode = (int)EOpCode.RunProcess };
                var exec = new RunProcess
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
            else if (message.Response is RunCompletedStatus)
            {
                var status = (RunCompletedStatus)message.Response;
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
            else if (message.Response is InstalledPrograms)
            {
                var progsList = (InstalledPrograms)message.Response;
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
        private static bool ReadConfigFile()
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
