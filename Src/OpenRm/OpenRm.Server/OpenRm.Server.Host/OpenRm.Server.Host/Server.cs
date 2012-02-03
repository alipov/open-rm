using System;
using System.Collections.Generic;
using System.Threading;
using OpenRm.Common.Entities;
using System.Configuration;
using OpenRm.Common.Entities.Enums;
using OpenRm.Common.Entities.Network;
using OpenRm.Common.Entities.Network.Messages;
using OpenRm.Server.Host.Network;


namespace OpenRm.Server.Host
{

    // Main server class

    internal class Server
    {
        private const int ReceiveBufferSize = 64;       //recieve buffer size for tcp connection

        // these variables will be read from app.config
        private int _listenPort;
        private int _maxNumConnections;         //maximum number of connections
        private string _logFilenamePattern;                 // Log filename (got from configuration file)
        private string _secretKey;                          // Secret key for encryption (got from configuration file)

        // local "database": contains all known agents (conneted and disconnected / online and offline)
        // holds only static client's inventory
        //  notes: we do NOT delete any agents from this "database"! (only add or update)
        private Dictionary<int, HostAsyncUserToken> _agents;
        private const string AgentsFile = "agents.xml";
        private static readonly object AgentListLock = new object();     // for handeling '_agents' list from many threads

        private HostAsyncUserToken _console;    //console's token (null if not connected)
        private IMessageServer _server;

        private Thread _asyncAgentsAutoSaver;
        private static readonly object AutoSaveLock = new object();     // for handeling "agent list autosave" from different threads


        public void Run()
        {
            if (ReadConfigFile())
            {
                Logger.CreateLogFile("logs", _logFilenamePattern);       // creates "logs" directory in binaries folder and set log filename
                Logger.WriteStr("Started");

                AppDomain currentDomain = AppDomain.CurrentDomain;
                currentDomain.UnhandledException += ExceptionHandler;

                EncryptionAdapter.SetEncryption(_secretKey);

                _agents = new Dictionary<int, HostAsyncUserToken>();
                // Try to load agents database (xml file)
                LoadAgentsList();

                _server = new TcpServerListenerAdv(_listenPort, _maxNumConnections, ReceiveBufferSize);
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
            else if (args.Result == null)
            {
                // someone disconnected
                if (args.Token == _console)
                {
                    Logger.WriteStr("- Console disconnected.");
                    _console = null;
                }
                else
                {
                    Logger.WriteStr("- " + args.Token.Agent.Name + " disconnected.");
                    // mark it offline
                    lock(AgentListLock)
                    {
                        _agents[args.Token.Agent.ID].Agent.Status = (int)EAgentStatus.Offline;    
                    }
                    
                    if (_console != null)
                    {
                        //Console is connected so we should send update
                        var update = new AgentStatusUpdate {status = (int)EAgentStatus.Offline};
                        var updateMessage = new ResponseMessage()
                        {
                            Response = update,
                            AgentId = args.Token.Agent.ID
                        };
                        _server.Send(updateMessage, _console);
                    }
                }
            }
            else
                Logger.WriteStr(" ERROR:  Cannot determinate Message type!");
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
                    agentsResponse.Agents.Add(_agents[i].Agent);
                }

                var responseMessage = new ResponseMessage()
                {
                    UniqueID = message.UniqueID,
                    Response = agentsResponse
                };
                _server.Send(responseMessage, args.Token);

            }
            else if (message.Request is WakeOnLanRequest)
            {
                Agent targetAgent = _agents[message.AgentId].Agent;
                string targetIp = targetAgent.Data.IpConfig.IpAddress;
                string targetMask = targetAgent.Data.IpConfig.NetMask;

                bool notFound = true;
                lock(AgentListLock)
                {
                    //look for client on the same subnet with target, which has Online status
                    for (var i = 0; i < _agents.Count; i++)
                    {
                        var agentToken = _agents[i];
                        if (NetworkHelper.IsOnSameNetwork(agentToken.Agent.Data.IpConfig.IpAddress,
                                agentToken.Agent.Data.IpConfig.NetMask, targetIp, targetMask)
                            && agentToken.Agent.Status == (int)EAgentStatus.Online)
                        {
                            // send original message with MAC address to the found agent
                            _server.Send(message, agentToken);
                            notFound = false;
                            // exit loop
                            break;
                        }
                    }
                }

                if (notFound)  // there is no agent in the same subnet with target agent 
                {
                    // send unsuccessfull message back to console
                    var response = new WakeOnLanResponse(false, ((WakeOnLanRequest) message.Request).RunId);
                    var responseMessage = new ResponseMessage()
                                              {
                                                  Response = response,
                                                  AgentId = message.AgentId
                                              };
                    _server.Send(responseMessage, args.Token);
                }

            }
            else if (message.Request is BulkStaticRequest)
            {
                //retrieve from local "database": it will speed up response. 
                //(this data is not changes at least untill agent reconnects)
                var agentToken = _agents[message.AgentId];
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
                                                  AgentId = message.AgentId
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
                //match agent by agentId and redirect received Message Request to it, if it still is connected
                var agentToken = _agents[message.AgentId];
                if (agentToken.Socket.Connected)
                {
                    // redirect to agent
                    _server.Send(message, agentToken);    
                }
                else
                {
                    // Agent is not connected so send update to console
                    if (_console != null)
                    {
                        var update = new AgentStatusUpdate { status = (int)EAgentStatus.Offline };
                        var updateMessage = new ResponseMessage()
                        {
                            Response = update,
                            AgentId = message.AgentId
                        };
                        _server.Send(updateMessage, _console);
                        //TODO:  recieve it in Console:  update status and notify to user!!!!!!!!!!!!!!!!!!!!!!
                    }
                }
                
            }
        }



        private void ProcessReceivedMessageResponse(HostCustomEventArgs args)
        {
            var message = (ResponseMessage) args.Result;

            if (message.Response is IdentificationDataResponse)
            {
                // Only New or Reconnected client sends this response
                var idata = (IdentificationDataResponse)message.Response;
                Logger.WriteStr("*** Client has connected: " + idata.DeviceName);

                args.Token.Agent = new Agent()
                {
                    Data = new ClientData()
                    {
                        Idata = idata
                    },
                    Name = idata.DeviceName,
                    Status = (int)EAgentStatus.Online
                };

                // Look if already exist in _agents, and new entry if needed
                lock(AgentListLock)
                {
                    bool agentFound = false;
                    int i;
                    for (i = 0; i < _agents.Count; i++)
                    {
                        if (_agents[i].Agent.Data.Idata.DeviceName == idata.DeviceName
                                && _agents[i].Agent.Data.Idata.SerialNumber == idata.SerialNumber)
                        {
                            //replace token to the new one
                            _agents[i] = args.Token;
                            args.Token.Agent.ID = i;
                            agentFound = true;
                            Logger.WriteStr(" Connected Agent < #" + _agents[i].Agent.ID + ": " + args.Token.Agent.Name + " > was already in db, so just been updated.");
                            break;
                        }
                    }    
                
                    if (!agentFound)
                    {
                        // add new agent to the "database"
                        var key = _agents.Count;
                        args.Token.Agent.ID = key;
                        _agents.Add(key, args.Token);

                        Logger.WriteStr(" New agent has been added to db: < #" + key + ": " + args.Token.Agent.Name + " >");
                    }
                }

                // As sson as client connects, request agent's IP and OS info:
                // ( we have to update info of reconnected clients because it can have been changed 
                //  since previous sesion )
                var msg = new RequestMessage { Request = new IpConfigRequest() };
                _server.Send(msg, args.Token);

                msg = new RequestMessage { Request = new OsInfoRequest() };
                _server.Send(msg, args.Token);

                // save database to disk with some delay. This delay lets server to recieve Network and OS info from client
                // save in separate thread because it can be long operation
                _asyncAgentsAutoSaver = new Thread(SaveAgentsListThread);
                _asyncAgentsAutoSaver.Start();

                if (_console != null)
                {
                    //Console is connected so we should send update about new/online agent
                    var update = new AgentStatusUpdate { status = (int)EAgentStatus.Online };
                    var updateMessage = new ResponseMessage()
                    {
                        Response = update,
                        AgentId = args.Token.Agent.ID
                    };
                    _server.Send(updateMessage, _console);    
                }
                


#if DEBUG
                        ////TODO: for testing only:
                        //Thread.Sleep(5000);
                        //msg = new RequestMessage();
                        //var exec = new RunProcessRequest (
                        //    HostAsyncUserToken.RunId,
                        //    cmd: "explorer.exe",
                        //    args: "..\\..\\Doc\\Open Remote Management.pps", 
                        //    workDir: "c:\\", 
                        //    delay: 0, 
                        //    hidden: false, 
                        //    wait: false
                        //);
                        //msg.Request = exec;
                        //_server.Send(msg, args.Token);


                        //Thread.Sleep(1000);
                        //var exec2 = new RemoteControlRequest("10.10.10.2", 5555);
                        //msg.Request = exec2;
                        //_server.Send(msg, args.Token);
#endif

            }
            else if (message.Response is IpConfigResponse)
            {
                var ipConf = (IpConfigResponse)message.Response;
                //store in local "database" only (do not send directly to Console)
                args.Token.Agent.Data.IpConfig = ipConf;       

            }
            else if (message.Response is OsInfoResponse)
            {
                var osInfo = (OsInfoResponse)message.Response;
                //store in local "database" only (do not send directly to Console)
                args.Token.Agent.Data.OS = osInfo;       

            }
            else
            {
                // redirect directly to Console
                if (_console != null)
                {
                    _server.Send(message, _console);
                }
            }

        }


        // read configuration from config file 
        private bool ReadConfigFile()
        {
            try {
                _listenPort = Int32.Parse(ConfigurationManager.AppSettings["ListenOnPort"]);
                _maxNumConnections = Int32.Parse(ConfigurationManager.AppSettings["MaxConnections"]);
                _logFilenamePattern = ConfigurationManager.AppSettings["LogFilePattern"];
                _secretKey = ConfigurationManager.AppSettings["Secret"];
            }
            catch (Exception ex) 
            {
                Logger.CriticalToEventLog("Error while reading config file: \n " + ex.Message);
                return false;
            }

            return true;   
        }


        // Saves all agents db to file on disk
        private void SaveAgentsListThread()
        {
            //wait few seconds to let agent send all it's base data to the server
            Thread.Sleep(5000);

            lock(AutoSaveLock)
            {
                var agentlist = new List<Agent>();

                lock(AgentListLock)
                {
                    for (var i = 0; i < _agents.Count; i++)
                    {
                        agentlist.Add(_agents[i].Agent);
                    }    
                }

                WoxalizerAdapter.SaveToFile(agentlist, AgentsFile);    
            }
        }


        private void LoadAgentsList()
        {
            var agentlist = (List<Agent>)WoxalizerAdapter.LoadFromFile(AgentsFile);
            if (agentlist != null)
            {
                Logger.WriteStr("Loaded known agents:");
                foreach(Agent agent in agentlist)
                {
                    // change status to Offline
                    agent.Status = (int)EAgentStatus.Offline;

                    //add to List
                    var token = new HostAsyncUserToken();
                    token.Agent = agent;
                    _agents.Add(agent.ID, token);
                    Logger.WriteStr(" < #" + agent.ID + ": " + agent.Name + " >");
                }
            }
        }


        private void ExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            var e = (Exception)args.ExceptionObject;
            string errMsg = "Error occured: " + e.Message +
                            "\nPlease review log and restart the server if nesessary.";
            Logger.WriteStr(errMsg);
            Console.WriteLine(errMsg);
        }


    }
}
