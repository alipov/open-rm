using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
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

        //private readonly string _serverIp;
        //private readonly int _serverPort;
        private IPEndPoint _endPoint;

        private readonly ManualResetEvent _clientDone = new ManualResetEvent(false);

        public TcpClient()
            : base(64)
        {
            // Initialize buffers for sending and receiving data by TCP layer. 
            _sendBuffer = new byte[64];
            _receiveBuffer = new byte[64];
        }

        //public TcpClient(string serverIp, int serverPort)
        //    : this(serverIp, serverPort, 64) { }

        //public TcpClient(string serverIp, int serverPort, int bufferSize)
        //    : base(bufferSize)
        //{
            
        //    _serverIp = serverIp;
        //    _serverPort = serverPort;
        //}

        public void Connect(IPEndPoint endPoint, Action<CustomEventArgs> callback)
        {
            _endPoint = endPoint;

            // point Args UserTokens to the same token
            var token = new GeneralUserToken
                    (new Socket(_endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp), callback);

            // Create two Args objects - one for sending, one for recieving data
            readArgs = new SocketAsyncEventArgs
                           {
                               RemoteEndPoint = _endPoint,
                               UserToken = token,
                               AcceptSocket = token.Socket
                           };
            readArgs.Completed += CommonCallback;

            writeArgs = new SocketAsyncEventArgs
                            {
                                RemoteEndPoint = readArgs.RemoteEndPoint,
                                UserToken = token,
                                AcceptSocket = token.Socket
                            };
            writeArgs.Completed += CommonCallback;

            // Save links to Args objects in the token
            token.readEventArgs = readArgs;
            token.writeEventArgs = writeArgs;

            token.Socket.ConnectAsync(writeArgs);

            // pause current thread
            _clientDone.WaitOne();
        }

        // A single callback is used for all socket operations. This method forwards execution on to the correct handler 
        // based on the type of completed operation.
        private void CommonCallback(object sender, SocketAsyncEventArgs e)
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
        private void ProcessConnect(SocketAsyncEventArgs args)
        {
            var token = (GeneralUserToken)args.UserToken;

            switch (args.SocketError)
            {
                case SocketError.Success:
                    Logger.WriteStr(string.Format("Successfully connected to the server on {0}", 
                                                                    token.Socket.LocalEndPoint));
                    _retryIntervalCurrent = RetryIntervalInitial;   // set it to initial value

                    // Set buffers:
                    // (buffers must be set AFTER connection is established)
                    token.writeEventArgs.SetBuffer(_sendBuffer, 0, _sendBuffer.Length);
                    token.readEventArgs.SetBuffer(_receiveBuffer, 0, _receiveBuffer.Length);

                    //Send authorization info about this client as soon as connection established
                    //var action = new ActionExecutor();
                    //action.PerformAction(token, new IdentificationDataRequest());

                    if (token.Callback != null)
                        token.Callback.Invoke(new CustomEventArgs(args.SocketError, null));

                    //Start waiting for incoming data
                    WaitForReceiveMessage(token.readEventArgs);
                    break;
                default:
                    var ex = (int)args.SocketError;
                    Logger.WriteStr
                        (string.Format("Cannot connect to server {0}. Will try again in {1} seconds", 
                         args.RemoteEndPoint, _retryIntervalCurrent));
                    Logger.WriteStr(string.Format("   (Exception: {0})", ex.ToString()));

                    Thread.Sleep(_retryIntervalCurrent * 1000);

                    //increase interval between reconnects up to retryIntervalMaximum value.
                    if (_retryIntervalCurrent < RetryIntervalMaximum)     
                        _retryIntervalCurrent += 5;         

                    args.SocketError = 0;  //clear Error info and try to connect again
                    token.Socket.ConnectAsync(args);
                    break;
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
            Connect(_endPoint, null); //TODO
            
        }

        protected override void ProcessReceivedMessageRequest(SocketAsyncEventArgs readEventArgs, RequestMessage msg)
        {
            var token = (AgentAsyncUserToken) readEventArgs.UserToken;

            //TODO: doesn't compiles. check what's wrong
            //var exec = new ActionExecutor();        //TODO: replace to static use

            //if (msg.Request is PerfmonDataRequest)
            //    exec.PerformAction(token, msg.Request);
            //else
            //{
            //    Thread sendingThread = new Thread(() => exec.PerformAction(token, msg.Request));
            //    sendingThread.Start();
            //}
        }

        protected override void ProcessReceivedMessageResponse(SocketAsyncEventArgs e, ResponseMessage message)
        {
            //TODO: what info client needs from server?
        }
    }
}
