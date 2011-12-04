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
        private const int ReceiveBufferSize = 64;       //recieve buffer size for tcp connection

        // these variables will be read from app.config
        private int _listenPort;
        private int _maxNumConnections;     //maximum number of connections
        private string _logFilenamePattern;

        // local "database": contains all known agents (conneted and disconnected / online and offline)
        // holds only static client's inventory
        //  notes: we do NOT delete any agents from this "database"! (only add or update)
        private Dictionary<int, HostAsyncUserToken> _agents;
        private int _agentsCount;   //last index

        private HostAsyncUserToken _console;    //console's token (null if not connected)
        private IMessageServer _server;


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
                // send all agents list to console (icluding static agent info: name, ip, OS, etc...)
                var agentsResponse = new ListAgentsResponse()
                {
                    Agents = new List<Agent>()
                };

                for (var i = 0; i < _agents.Count; i++)
                {
                    var thisAgent = new Agent()
                        {
                            ID = i,
                            Name = _agents[i].Agent.Data.Idata.deviceName,
                            Status = _agents[i].Agent.Status
                        };

                    agentsResponse.Agents.Add(agent.Value.Agent);
                }

                var responseMessage = new ResponseMessage()
                {
                    Response = agentsResponse
                };
                _server.Send(responseMessage, args.Token);

            }
            else if (message.Request is WakeOnLanRequest)
            {
                Agent targetAgent = _agents[message.agentId].Agent;
                string targetIp = targetAgent.Data.IpConfig.IpAddress;
                string targetMask = targetAgent.Data.IpConfig.netMask;

                bool _notFound = true;

                //look for client on the same subnet with target, which has Online status
                for (var i = 0; i < _agents.Count; i++)
                {
                    var agentToken = _agents[i];
                    if (NetworkHelper.IsOnSameNetwork(agentToken.Agent.Data.IpConfig.IpAddress,
                            agentToken.Agent.Data.IpConfig.netMask, targetIp, targetMask)
                        && agentToken.Agent.Status == EAgentStatus.Online)
                    {
                        // send original message with MAC address to the found agent
                        _server.Send(message, agentToken);
                        _notFound = false;
                        // exit loop
                        break;
                    }
                }
                if (_notFound)  // there is no agent in the same subnet with target agent 
                {
                    // send unsuccessfull message back to console
                    var response = new WakeOnLanResponse(false, ((WakeOnLanRequest) message.Request).RunId);
                    var responseMessage = new ResponseMessage()
                                              {
                                                  Response = response,
                                                  agentId = message.agentId
                                              };
                    _server.Send(responseMessage, args.Token);
                }

            }
            else if (message.Request is BulkStaticRequest)
            {
                //retrieve from local "database": it will speed up response. 
                //(this data is not changes at least untill agent reconnects)
                var agentToken = _agents[message.agentId];
                if (agentToken.Agent.Data.IpConfig != null && agentToken.Agent.Data.OS != null)
                {
                    var response = new BulkStaticResponse()
                                       {
                                           IpConf = agentToken.Agent.Data.IpConfig,
                                           OsInfo = agentToken.Agent.Data.OS
                                       };
                    var responseMessage = new ResponseMessage()
                                              {
                                                  Response = response,
                                                  agentId = message.agentId
                                              };
                    _server.Send(responseMessage, args.Token);
                }
                else
                {
                    // nas no reqired info, so send request to client
                    _server.Send(message, agentToken);
                }
            }
            else
            {
                //match agent by agentId and redirect received Message Request to it
                var agentToken = _agents[message.agentId];
                _server.Send(message, agentToken);

            }


        }


        private void ProcessReceivedMessageResponse(HostCustomEventArgs args)
        {
            var message = (ResponseMessage) args.Result;

            if (message.Response is IdentificationDataResponse)
            {
                // Only New or Reconnected client sends this response
                var idata = (IdentificationDataResponse)message.Response;
                Logger.WriteStr(" * Client has connected: " + idata.deviceName);
                
                // Look if already exist in _agents, and new entry if needed
                if (_agents.All(a => a.Value.Agent.Data.Idata.deviceName != idata.deviceName))
                {
                    args.Token.Agent = new Agent()
                                           {
                                               Data = new ClientData()
                                                          {
                                                              Idata = idata
                                                          }
                                           };
                    var key = Interlocked.Increment(ref _agentsCount);
                    _agents.Add(key, args.Token);
                    args.Token.Agent.ID = key;
                    args.Token.Agent.Name = args.Token.Agent.Data.Idata.deviceName;
                }

                // Request agent's IP and OS info:
                // ( we have to update info of reconnected clients because it can have been changed 
                //  since previous sesion )
                var msg = new RequestMessage { Request = new IpConfigRequest() };
                _server.Send(msg, args.Token);

                msg = new RequestMessage { Request = new OsInfoRequest() };
                _server.Send(msg, args.Token);


                        ////TODO: for testing only:
                        //msg = new RequestMessage { OpCode = (int)EOpCode.RunProcess };
                        //var exec = new RunProcessRequest
                        //{
                        //    RunId = HostAsyncUserToken.RunId,
                        //    Cmd = "notepad.exe",
                        //    Args = "",
                        //    WorkDir = "c:\\",
                        //    TimeOut = 180000,        //ms
                        //    Hidden = true
                        //};
                        //msg.Request = exec;
                        //_server.Send(msg, args.Token);

            }
            else if (message.Response is IpConfigResponse)
            {
                var ipConf = (IpConfigResponse)message.Response;
                args.Token.Agent.Data.IpConfig = ipConf;       //store in local "database" only (do not send directly to Console)

            }
            else if (message.Response is OsInfoResponse)
            {
                var osInfo = (OsInfoResponse)message.Response;
                args.Token.Agent.Data.OS = osInfo;       //store in local "database" only (do not send directly to Console)



                //TODO: move to another place
                //var msg = new RequestMessage { OpCode = (int)EOpCode.RunProcess };
                //var exec = new RunProcessRequest
                //{
                //    RunId = HostAsyncUserToken.RunId,
                //    Cmd = "notepad.exe",
                //    Args = "",
                //    WorkDir = "c:\\",
                //    TimeOut = 180000,        //ms
                //    Hidden = true
                //};

                //msg.Request = exec;
                //_server.Send(msg, args.Token);
            }
            else
            {
                // redirect directly to Console
                if (_console != null)
                {
                    _server.Send(message, _console);
                }
            }




            //TODO:  Move to GUI:
            //else if (message.Response is RunProcessResponse)
            //{
            //    var status = (RunProcessResponse)message.Response;
            //        
            //        if (status.ExitCode == 0)
            //        {
            //            Logger.WriteStr("Remote successfully executed");
            //        }
            //        else if (status.ExitCode > 0)
            //        {
            //            Logger.WriteStr("Remote program executed with exit code: " + status.ExitCode +
            //                            "and error message: \"" + status.ErrorMessage + "\"");
            //        }
            //        else
            //        {
            //            throw new ArgumentException("Invalid exit code of remote execution (" + status.ExitCode + ") / Remote process has not been executed");
            //        }
            //}

                ////TODO: for testing only:
                //var msg = new RequestMessage { OpCode = (int)EOpCode.InstalledPrograms };
                //_server.Send(msg, args.Token);

            //else if (message.Response is InstalledProgramsResponse)
            //{
            //    var progsList = (InstalledProgramsResponse)message.Response;
            //    foreach (string s in progsList.Progs)
            //    {
            //        Console.WriteLine(s);
            //    }
            //
            //    //}
            //    //else if (message.Response is )
            //    //{
            //    //............................
            //    //
            //    //...
            //}
            //else
            //{
            //    Logger.WriteStr(string.Format("WARNING: Recieved unkown response from {0}!", 
            //                                                    args.Token.Socket.RemoteEndPoint));
            //}

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
