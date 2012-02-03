using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using OpenRm.Common.Entities.Network.Messages;

namespace OpenRm.Common.Entities.Network
{
    public class GeneralSocketClient : TcpBase, IMessageClient
    {
        private IPEndPoint _endPoint;
        private GeneralUserToken _userToken;
        private bool _isConnected;
        private Socket _socket;

        // buffers for sending/receiving data by TCP layer. 
        // it has fixed size (the same as in server, but it can be different)
        private byte[] _sendBuffer;
        private byte[] _receiveBuffer;
        private static int _tcpBufferSize = 64;      // buffers size (hardcoded but can be changed)

        // async objects for sending and receiving data
        private SocketAsyncEventArgs readArgs;
        private SocketAsyncEventArgs writeArgs;

        public bool IsConnected 
        { 
            get
            {
                return _isConnected;
            }
        }

        
        public GeneralSocketClient()
            : base(_tcpBufferSize) 
        {
            // Initialize buffer for sending/receiving data by TCP layer. 
            _sendBuffer = new byte[_tcpBufferSize];
            _receiveBuffer = new byte[_tcpBufferSize];
        }


        public void Connect(IPEndPoint endPoint, Action<CustomEventArgs> callback)
        {
            if (_isConnected)
                return;
                //TODO:  check if exception needed: throw new SocketException((int)SocketError.IsConnected);

            _endPoint = endPoint;

            _socket = new Socket(_endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // point Args UserTokens to the same token
            _userToken = new GeneralUserToken
                    (_socket, callback);

            // Create two Args objects - one for sending, one for recieving data
            readArgs = new SocketAsyncEventArgs
            {
                RemoteEndPoint = _endPoint,
                UserToken = _userToken,
                AcceptSocket = _userToken.Socket
            };
            readArgs.Completed += CommonCallback;

            writeArgs = new SocketAsyncEventArgs
            {
                RemoteEndPoint = readArgs.RemoteEndPoint,
                UserToken = _userToken,
                AcceptSocket = _userToken.Socket
            };
            writeArgs.Completed += CommonCallback;

            // Save links to Args objects in the token
            _userToken.readEventArgs = readArgs;
            _userToken.writeEventArgs = writeArgs;

            _userToken.writeEventArgs.SetBuffer(_sendBuffer, 0, _sendBuffer.Length);
            _userToken.readEventArgs.SetBuffer(_receiveBuffer, 0, _receiveBuffer.Length);

            _socket.ConnectAsync(writeArgs);
            
        }


        private void CommonCallback(object sender, SocketAsyncEventArgs args)
        {
            switch (args.LastOperation)
            {
                case SocketAsyncOperation.Connect:
                    OnConnectCompleted(sender, args);
                    break;
                case SocketAsyncOperation.Receive:
                    ProcessReceive(args);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(args);
                    break;
                case SocketAsyncOperation.Disconnect:
                    OnDisconnectCompleted(sender, args);
                    break;
                default:
                    CloseConnection(args);
                    throw new Exception("Invalid operation completed");
            }
        }

        private void OnConnectCompleted(object sender, SocketAsyncEventArgs args)
        {
            switch (args.SocketError)
            {
                case SocketError.Success:
                    _isConnected = true;

                    Logger.WriteStr(string.Format
                        ("Successfully connected to the server on {0}", _socket.LocalEndPoint));

                    // Start timer sending periodic keep-alive messages
                    StartKeepAlives();

                    if (_userToken.Callback != null)
                    {
                        var eventArgs = new CustomEventArgs(args.SocketError, null)
                        {
                            LocalEndPoint = (IPEndPoint)_socket.LocalEndPoint
                        };
                        _userToken.Callback.Invoke(eventArgs);
                    }

                    //Start waiting for incoming data
                    WaitForReceiveMessage(_userToken.readEventArgs);

                    break;
                default:
                    var ex = args.SocketError;

                    Logger.WriteStr(string.Format
                        ("Cannot connect to server {0}. (Exception: {1})", args.RemoteEndPoint, ex.ToString()));

                    //erase error
                    args.SocketError = 0;

                    if (_userToken.Callback != null)
                    {
                        var eventArgs = new CustomEventArgs(ex, null);
                        _userToken.Callback.Invoke(eventArgs);
                    }

                    break;
            }
        }


        private void StartKeepAlives()
        {
            // Start timer that send keep-alive messages
            _userToken.KeepAliveTimer = new KeepAliveTimer(_userToken);
            _userToken.KeepAliveTimer.Elapsed += SendKeepAlive;
        }


        public void Disconnect(Action<CustomEventArgs> callback)
        {
            // Prepare arguments for send/receive operation.
            var socketArgs = new SocketAsyncEventArgs
                                 {
                                     //UserToken = callback, 
                                     RemoteEndPoint = _endPoint
                                 };

            socketArgs.SetBuffer(new byte[0], 0, 0);
            socketArgs.Completed += CommonCallback;

            _socket.DisconnectAsync(socketArgs);
        }

        private void OnDisconnectCompleted(object sender, SocketAsyncEventArgs args)
        {
            if (args.SocketError != SocketError.Success)
            {
                CloseConnection(args);
            }

            _isConnected = false;

            var callback = args.UserToken as Action<CustomEventArgs>;

            if (callback != null)
                callback.Invoke(new CustomEventArgs(args.SocketError, null));
        }


        protected override void ProcessReceivedMessage(SocketAsyncEventArgs e)
        {
            // decrypt message
            byte[] decryptedMsgData = EncryptionAdapter.Decrypt(_userToken.RecievedMsgData);
            Logger.WriteStr(" Recieved message was decrypted as xml: " + utf8.GetString(decryptedMsgData));

            // deserialize to object
            Message message = null;
            try
            {
                message = WoxalizerAdapter.DeserializeFromXml(decryptedMsgData);
            }
            catch(Exception ex)
            {
                Logger.WriteStr("ERROR: Cannot deserialize recieved message (" + ex.Message + "). Aborting connection.");
                CloseConnection(e);
            }

            if (_userToken.Callback != null)
            {
                _userToken.Callback.Invoke(new CustomEventArgs(e.SocketError, message));
            }
        }


        public void Send(Message message, Action<CustomEventArgs> callback)
        {
            if (!_isConnected)
                return;
                //TODO:  check if exception needed: throw new SocketException((int)SocketError.NotConnected);

            _userToken.Callback = callback;

            byte[] messageToSend = WoxalizerAdapter.SerializeToXml(message);
            Logger.WriteStr(" The message (xml) to be sent: " + utf8.GetString(messageToSend));

            // encrypt message
            byte[] encryptedMsgData = EncryptionAdapter.Encrypt(messageToSend);

            SendMessage(_userToken, encryptedMsgData);

        }


        // This method is called on sending failure
        protected override void ProcessSendFailure(SocketAsyncEventArgs e)
        {
            ProcessFailure(e);
        }

        // This method is called on receiving failure
        protected override void ProcessReceiveFailure(SocketAsyncEventArgs e)
        {
            ProcessFailure(e);
        }

        // Processing send/receive failure.
        //  ProcessSendFailure and ProcessReceiveFailure are called whenever Async operation receives SocketError,
        //  so we can assume that there was serious problem such as closed, broken or bad connection,
        //  that's why we decide to close socket and reconnect to the server
        protected override void ProcessFailure(SocketAsyncEventArgs e)
        {
            var callback = _userToken.Callback;
            var error = e.SocketError;

            CloseConnection(e);

            if (callback != null)
            {
                callback.Invoke(new CustomEventArgs(error, null));
            }
        }


        protected override void CloseConnection(SocketAsyncEventArgs e)
        {
            _isConnected = false;

            try
            {
                //maybe timer has been stopped already
                _userToken.KeepAliveTimer.Stop();
            }
            catch (Exception) { }
            
             //close the socket
            try
            {
                _userToken.Socket.Shutdown(SocketShutdown.Send);
                _userToken.Socket.Close();
            }
            // throws if server has already closed connection
            catch (Exception) { }
            
            Logger.WriteStr("Disconnected from server.");

            // cleanup token's buffers
            _userToken.Wipe();

        }

    }
}
