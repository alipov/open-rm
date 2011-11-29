using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using OpenRm.Common.Entities;
using OpenRm.Common.Entities.Network;
using OpenRm.Common.Entities.Network.Messages;

namespace OpenRm.Agent
{    
    internal class TcpClient : NonInterfacedClientBase
    {
        // SocketAsyncEventArgs objects for write and read socket operations
        private SocketAsyncEventArgs readArgs;
        private SocketAsyncEventArgs writeArgs;

        // buffers for sending/receiving data by TCP layer. 
        // it has fixed size (the same as in server, but it can be different)
        private byte[] _sendBuffer;
        private byte[] _receiveBuffer;
                                                        
        // Interval between reconnects to server (when was unable to connect / has been disconnected)    
        private int _retryIntervalCurrent;

        private const int RetryIntervalInitial = 5; // 5 sec.
        private const int RetryIntervalMaximum = 60; // 60 sec.

        private readonly string _serverIp;
        private readonly int _serverPort;

        private readonly ManualResetEvent _clientDone = new ManualResetEvent(false);

        public TcpClient(string serverIp, int serverPort)
            : this(serverIp, serverPort, 64) { }

        public TcpClient(string serverIp, int serverPort, int bufferSize)
            : base(bufferSize)
        {
            // Initialize buffers for sending and receiving data by TCP layer. 
            _sendBuffer = new byte[bufferSize];
            _receiveBuffer = new byte[bufferSize];
            _serverIp = serverIp;
            _serverPort = serverPort;
        }

        public void Start()
        {
            var socket = new Socket((IPAddress.Parse(_serverIp)).AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // Create two Args objects - one for sending, one for recieving data
            readArgs = new SocketAsyncEventArgs();
            writeArgs = new SocketAsyncEventArgs();
            readArgs.Completed += SocketEventArg_Completed;
            writeArgs.Completed += SocketEventArg_Completed;
            readArgs.RemoteEndPoint = new IPEndPoint(IPAddress.Parse(_serverIp), _serverPort);
            writeArgs.RemoteEndPoint = readArgs.RemoteEndPoint;

            // point Args UserTokens to the same token
            var token = new AgentAsyncUserToken();
            readArgs.UserToken = token;
            writeArgs.UserToken = token;

            token.Socket = socket;
            readArgs.AcceptSocket = socket;
            writeArgs.AcceptSocket = socket;

            // Save links to Args objects in the token
            token.readEventArgs = readArgs;
            token.writeEventArgs = writeArgs;

            socket.ConnectAsync(writeArgs);

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

                var token = (AgentAsyncUserToken) e.UserToken;

                // Set buffers:
                // (buffers must be set AFTER connection is established)
                token.writeEventArgs.SetBuffer(_sendBuffer, 0, _sendBuffer.Length);
                token.readEventArgs.SetBuffer(_receiveBuffer, 0, _receiveBuffer.Length);

                //Send authorization info about this client as soon as connection established
                var action = new ActionExecutor();
                action.PerformAction(token, new IdentificationDataRequest());

                //Start waiting for incoming data
                WaitForReceiveMessage(token.readEventArgs);

            }
            else
            {
                int ex = (int)e.SocketError;
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

            token.Wipe();
            token.Socket.Close();

            Logger.WriteStr("Client should be running so just try to reconnect to server...");
            _retryIntervalCurrent = RetryIntervalInitial;  //reset to initial value
                
            //Reconnect to server
            Start();
            
        }

        protected override void ProcessReceivedMessageRequest(SocketAsyncEventArgs readEventArgs, RequestMessage msg)
        {
            var token = (AgentAsyncUserToken) readEventArgs.UserToken;

            Thread sendingThread = new Thread(() => ActionExecutor.PerformAction(token, msg.Request));
            sendingThread.Start();

        }

        protected override void ProcessReceivedMessageResponse(SocketAsyncEventArgs e, ResponseMessage message)
        {
            //TODO: what info client needs from server?
        }


    }
}
