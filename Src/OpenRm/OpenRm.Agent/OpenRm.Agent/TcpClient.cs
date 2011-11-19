using System;
using System.Reflection;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using OpenRm.Common.Entities;
using OpenRm.Common.Entities.Network;
using OpenRm.Common.Entities.Network.Messages;

namespace OpenRm.Agent
{    
    internal class TcpClient : TcpBase
    {
        // buffer for sending/receiving data by TCP layer. 
        // Note that it has fixed size (same as for server, but also can be different)
        private readonly byte[] _sendReceiveBuffer;
                                                        
        // Interval between reconnects to server (when was unable to connect / has been disconnected)    
        private int _retryIntervalCurrent;

        private const int RetryIntervalInitial = 5; // 5 sec.
        private const int RetryIntervalMaximum = 60; // 60 sec.

        private readonly string _serverIp;
        private readonly int _serverPort;

        private readonly ManualResetEvent _clientDone = new ManualResetEvent(false);

        public TcpClient(string serverIp, int serverPort, Func<object, ResolveEventArgs, Assembly> resolver)
            : this(serverIp, serverPort, 64, resolver) { }

        public TcpClient(string serverIp, int serverPort, int bufferSize, Func<object, ResolveEventArgs, Assembly> resolver)
            : base(resolver, bufferSize)
        {
            // Initialize buffer for sending/receiving data by TCP layer. 
            _sendReceiveBuffer = new byte[bufferSize];
            _serverIp = serverIp;
            _serverPort = serverPort;
        }

        public override void Start()
        {
            var socket = new Socket((IPAddress.Parse(_serverIp)).AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            var sockArgs = new SocketAsyncEventArgs();
            sockArgs.Completed += SocketEventArg_Completed;
            sockArgs.RemoteEndPoint = new IPEndPoint(IPAddress.Parse(_serverIp), _serverPort);
            sockArgs.UserToken = new AgentAsyncUserToken();
            ((AgentAsyncUserToken)sockArgs.UserToken).Socket = socket;
            sockArgs.AcceptSocket = socket;

            socket.ConnectAsync(sockArgs);

            //EstablishConnection();

            // pause current thread
            _clientDone.WaitOne();
        }

        // A single callback is used for all socket operations. This method forwards execution on to the correct handler 
        // based on the type of completed operation.
        private void SocketEventArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Connect:
                    ProcessConnect(e);
                    break;
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
                default:
                    CloseConnection(e);
                    throw new Exception("Invalid operation completed");
            }
        }


        // Called when a ConnectAsync operation completes
        private void ProcessConnect(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                Logger.WriteStr("Successfully connected to the server on " + ((AgentAsyncUserToken)(e.UserToken)).Socket.LocalEndPoint.ToString());
                _retryIntervalCurrent = RetryIntervalInitial;          // set it to initial value

                //Send authorization info about this client as soon as connection established
                var idata = OpProcessor.GetInfo(); // fill required data
                var message = new ResponseMessage {Response = idata};
                SendMessage(e, WoxalizerAdapter.SerializeToXml(message, TypeResolving.AssemblyResolveHandler));
                
            }
            else
            {
                int ex = (int)e.SocketError;
                Console.WriteLine("Cannot connect to server " + e.RemoteEndPoint.ToString() + ". Will try again in " + _retryIntervalCurrent + " seconds");
                Logger.WriteStr("Cannot connect to server " + e.RemoteEndPoint.ToString() + ". Will try again in " + _retryIntervalCurrent + " seconds");
                Logger.WriteStr("   (Exception: " + ex.ToString() + ")");

                Thread.Sleep(_retryIntervalCurrent * 1000);
                if (_retryIntervalCurrent < RetryIntervalMaximum)     //increase interval between reconnects up to retryIntervalMaximum value.
                    _retryIntervalCurrent += 5;         

                e.SocketError = 0;  //clear Error info and try to connect again
                ((AgentAsyncUserToken)e.UserToken).Socket.ConnectAsync(e);

                return;
            }
        }

        protected override void StartSend(SocketAsyncEventArgs e)
        {
            var token = (AgentAsyncUserToken)e.UserToken;

            int bytesToTransfer = Math.Min(_sendReceiveBuffer.Length, token.SendingMsg.Length - token.SendingMsgBytesSent);
            e.SetBuffer(_sendReceiveBuffer, 0, bytesToTransfer);
            Array.Copy(token.SendingMsg, token.SendingMsgBytesSent, e.Buffer, e.Offset, bytesToTransfer);

            bool willRaiseEvent = e.AcceptSocket.SendAsync(e);
            if (!willRaiseEvent)
            {
                ProcessSend(e);
            }
        }


        protected override void CloseConnection(SocketAsyncEventArgs e)
        {
            var token = (AgentAsyncUserToken)e.UserToken;

            // close the socket
            try
            {
                token.Socket.Shutdown(SocketShutdown.Send);
            }
            // throws if server has already closed connection
            catch (Exception) { }

            Logger.WriteStr("Client disconnected from server.");

            token.Clean();
            token.Socket.Close();

            if (App.agentStarted)
            {
                Logger.WriteStr("Client should be running so just try to reconnect to server...");
                _retryIntervalCurrent = RetryIntervalInitial;  //reset to initial value
                
                //Reconnect to server
                Start();
            }
            else
            {
                token.Socket.Close();
                _clientDone.Reset();
                Console.WriteLine("Client has been terminated.");
            }
        }

        protected override void ProcessReceivedMessageRequest(SocketAsyncEventArgs e, RequestMessage message)
        {
            var token = (AgentAsyncUserToken)e.UserToken;

            ResponseMessage responseMsg;
            switch (message.OpCode)
            {
                case (int)EOpCode.IpConfigData:
                    var ipdata = new IpConfigData();
                    OpProcessor.GetInfo(ipdata, ((IPEndPoint)token.Socket.LocalEndPoint).Address.ToString());       // fill required data
                    responseMsg = new ResponseMessage { Response = ipdata };
                    break;

                case (int)EOpCode.RunProcess:
                    RunCompletedStatus result = OpProcessor.ExecuteProcess((RunProcess)message.Request);
                    responseMsg = new ResponseMessage { Response = result };
                    break;

                case (int)EOpCode.OsInfo:
                    var os = new OsInfo();
                    OpProcessor.GetInfo(os);
                    responseMsg = new ResponseMessage { Response = os };
                    break;

                case (int)EOpCode.PerfmonData:
                    var pf = new PerfmonData();
                    OpProcessor.GetInfo(pf, token.Data.OS.SystemDrive);     //provide which disk to monitor
                    responseMsg = new ResponseMessage { Response = pf };
                    break;

                case (int)EOpCode.InstalledPrograms:
                    var progs = new InstalledPrograms();
                    progs.Progs = OpProcessor.GetInstalledPrograms();
                    responseMsg = new ResponseMessage { Response = progs };
                    break;

                    //...


                //    //TODO:  Add all OpCodes...

                    break;
                default:
                    throw new ArgumentException("WARNING: Got unknown operation code request!");
            }
            SendMessage(e, WoxalizerAdapter.SerializeToXml(responseMsg, TypeResolving.AssemblyResolveHandler));
        }

        protected override void ProcessReceivedMessageResponse(SocketAsyncEventArgs e, ResponseMessage message)
        {
            //TODO: what info client needs from server?
        }
    }
}
