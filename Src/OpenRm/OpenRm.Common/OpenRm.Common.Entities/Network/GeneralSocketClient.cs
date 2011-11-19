using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using OpenRm.Common.Entities.Network.Messages;

namespace OpenRm.Common.Entities.Network
{
    public class GeneralSocketClient : TcpBase, IMessageClient
    {
        private readonly byte[] _sendReceiveBuffer;
        private IPEndPoint _endPoint;
        private GeneralUserToken _userToken;
        private bool _isConnected;
        private Socket _socket;

        public bool IsConnected 
        { 
            get
            {
                return _isConnected;
            }
        }

        // Interval between reconnects to server (when was unable to connect / has been disconnected)    
        private int _retryIntervalCurrent;

        private const int RetryIntervalInitial = 5; // 5 sec.
        private const int RetryIntervalMaximum = 60; // 60 sec.

        public GeneralSocketClient()
            : base (null, 64) // TODO: remove the hardcoded value
        {
            // Initialize buffer for sending/receiving data by TCP layer. 
            _sendReceiveBuffer = new byte[64];
        }

        //public GeneralSocketClient(int bufferSize)
        //    : base(null, bufferSize)
        //{
        //    // Initialize buffer for sending/receiving data by TCP layer. 
        //    _sendReceiveBuffer = new byte[bufferSize];
        //}

        public void Connect(IPEndPoint endPoint, Action<CustomEventArgs> callback)
        {
            if(_isConnected)
                throw new SocketException((int)SocketError.IsConnected);

            _endPoint = endPoint;
            _socket = new Socket(_endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            var sockArgs = new SocketAsyncEventArgs
                               {
                                   RemoteEndPoint = _endPoint,
                                   //UserToken = new GeneralUserToken(socket),
                                   //AcceptSocket = socket
                               };
            sockArgs.Completed += CommonCallback;

            _userToken = new GeneralUserToken(null, callback);

            _socket.ConnectAsync(sockArgs);
        }

        public void Send(Message message, Action<CustomEventArgs> callback)
        {
            if (!_isConnected)
                throw new SocketException((int)SocketError.NotConnected);
            
            // Create a buffer to send.
            Byte[] sendBuffer = WoxalizerAdapter.SerializeToXml(message, null);

            // Prepare arguments for send/receive operation.
            var socketArgs = new SocketAsyncEventArgs
                                 {
                                     RemoteEndPoint = _endPoint,
                                     //UserToken = _userToken
                                 };
            socketArgs.Completed += CommonCallback;
            socketArgs.SetBuffer(sendBuffer, 0, sendBuffer.Length);
            
            _userToken = new GeneralUserToken(null, callback);

            StartSend(socketArgs);
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
                throw new SocketException((int)args.SocketError);
            }

            _isConnected = false;

            var callback = args.UserToken as Action<CustomEventArgs>;

            if (callback != null)
                callback.Invoke(new CustomEventArgs(args.SocketError, null));
        }


        protected override void StartSend(SocketAsyncEventArgs args)
        {
            int bytesToTransfer = Math.Min(_sendReceiveBuffer.Length, _userToken.SendingMsg.Length - _userToken.SendingMsgBytesSent);
            args.SetBuffer(_sendReceiveBuffer, 0, bytesToTransfer);
            Array.Copy(_userToken.SendingMsg, _userToken.SendingMsgBytesSent, args.Buffer, args.Offset, bytesToTransfer);

            bool willRaiseEvent = _socket.SendAsync(args);//args.AcceptSocket.SendAsync(args);
            if (!willRaiseEvent)
            {
                ProcessSend(args);
            }
        }

        protected override void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                // receiveBufferSize is the maximum data length in one send
                _userToken.SendingMsgBytesSent += ReceiveBufferSize;
                if (_userToken.SendingMsgBytesSent < _userToken.SendingMsg.Length)
                {
                    // Not all message has been sent, so send next part
                    Logger.WriteStr(string.Format
                        ("{0} of {1} have been sent. Calling additional Send...", 
                          _userToken.SendingMsgBytesSent, _userToken.SendingMsg.Length));

                    // send the rest of the message
                    StartSend(e);
                    return;
                }

                Logger.WriteStr(" Message has been sent.");

                // reset token's buffers and counters before reusing the token
                //_userToken.Clean();

                // read the answer send from the client
                StartReceive(e);

            }
            else
            {
                Logger.WriteStr(" Message has failed to be sent.");
                ////token.Clean();
                //CloseConnection(e);
                if (_userToken.Callback != null)
                {
                    _userToken.Callback.Invoke(new CustomEventArgs(e.SocketError, null));
                }
            }
        }

        // Invoked when an asycnhronous receive operation completes.  
        // If the remote host closed the connection, then the socket is closed.
        protected override void ProcessReceive(SocketAsyncEventArgs e)
        {
            // Check if the remote host closed the connection
            // BytesTransferred is the number of bytes transferred in the socket operation.
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                // got message. need to handle it
                Logger.WriteStr("Recieved data (" + e.BytesTransferred + " bytes)");

                // now we need to check if we have complete message in our recieved data.
                // if yes - process it
                // but few messages can be combined into one send/recieve operation,
                //  or we can get only half message or just part of Prefix 
                //  if we get part of message, we'll hold it's data in UserToken and use it on next Receive

                int i = 0;      // go through buffer of currently received data 
                while (i < e.BytesTransferred)
                {
                    // Determine how many bytes we want to transfer to the buffer and transfer them 
                    int bytesAvailable = e.BytesTransferred - i;
                    if (_userToken.MsgData == null)
                    {
                        // token.msgData is empty so we a dealing with Prefix.
                        // Copy the incoming bytes into token's prefix's buffer
                        // All incoming data is in e.Buffer, at e.Offset position
                        int bytesRequested = MsgPrefixLength - _userToken.RecievedPrefixPartLength;
                        int bytesTransferred = Math.Min(bytesRequested, bytesAvailable);
                        Array.Copy(e.Buffer, e.Offset + i, _userToken.PrefixData, _userToken.RecievedPrefixPartLength, bytesTransferred);
                        i += bytesTransferred;

                        _userToken.RecievedPrefixPartLength += bytesTransferred;

                        if (_userToken.RecievedPrefixPartLength != MsgPrefixLength)
                        {
                            // We haven't gotten all the prefix buffer yet: call Receive again.
                            Logger.WriteStr("We've got just a part of prefix. Waiting for more data to arrive...");
                            StartReceive(e);
                            return;
                        }

                        // We've gotten the prefix buffer 
                        int length = BitConverter.ToInt32(_userToken.PrefixData, 0);
                        Logger.WriteStr(" Got prefix representing value: " + length);

                        if (length < 0)
                            throw new ProtocolViolationException("Invalid message prefix");

                        // Save prefix value into token
                        _userToken.MessageLength = length;

                        // Create the data buffer and start reading into it 
                        _userToken.MsgData = new byte[length];

                        // zero prefix counter
                        _userToken.RecievedPrefixPartLength = 0;
                    }
                    else
                    {
                        // We're reading into the data buffer  
                        int bytesRequested = _userToken.MessageLength - _userToken.RecievedMsgPartLength;
                        int bytesTransferred = Math.Min(bytesRequested, bytesAvailable);
                        Array.Copy(e.Buffer, e.Offset + i, _userToken.MsgData, _userToken.RecievedMsgPartLength, bytesTransferred);
                        Logger.WriteStr("message till now: " + Encoding.ASCII.GetString(_userToken.MsgData));
                        i += bytesTransferred;

                        _userToken.RecievedMsgPartLength += bytesTransferred;
                        if (_userToken.RecievedMsgPartLength != _userToken.MsgData.Length)
                        {
                            // We haven't gotten all the data buffer yet: call Receive again to get more data
                            StartReceive(e);
                            return;
                        }

                        // we've gotten an entire packet
                        Logger.WriteStr(string.Format("Got complete message from {0}: {1}", 
                            _socket.RemoteEndPoint, Encoding.ASCII.GetString(_userToken.MsgData)));

                        //ProcessReceivedMessage(e);
                        Message message = WoxalizerAdapter.DeserializeFromXml(_userToken.MsgData, null);
                        if (_userToken.Callback != null)
                        {
                            _userToken.Callback.Invoke(new CustomEventArgs(e.SocketError, message));
                        }
                    }
                }
            }
            else
            {
                Logger.WriteStr(string.Format
                    ("ERROR: Failed to get data on socket {0} due to exception:\n{1}\nClosing this connection....", 
                                    _socket.LocalEndPoint, new SocketException((int)e.SocketError)));
                //CloseConnection(e);
                if (_userToken.Callback != null)
                {
                    _userToken.Callback.Invoke(new CustomEventArgs(SocketError.Fault, null));
                }
            }
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

        private void OnConnectCompleted(object sender, SocketAsyncEventArgs e)
        {
            switch (e.SocketError)
            {
                case SocketError.Success:
                    _isConnected = true;

                    Logger.WriteStr(string.Format
                        ("Successfully connected to the server on {0}", _socket.LocalEndPoint));
                    
                    _retryIntervalCurrent = RetryIntervalInitial;

                    if (_userToken.Callback != null)
                        _userToken.Callback.Invoke(new CustomEventArgs(e.SocketError, null));
                    break;
                default:
                    var ex = (int)e.SocketError;

                    Logger.WriteStr(string.Format
                        ("Cannot connect to server {0}. Will try again in {1} seconds", 
                                                e.RemoteEndPoint, _retryIntervalCurrent));
                    Logger.WriteStr(string.Format("   (Exception: {0})", ex.ToString()));

                    Thread.Sleep(_retryIntervalCurrent * 1000);

                    //increase interval between reconnects up to retryIntervalMaximum value.
                    if (_retryIntervalCurrent < RetryIntervalMaximum)     
                        _retryIntervalCurrent += 5;

                    //TODO: add maximum reconnects

                    // try to connect again
                    e.SocketError = 0;
                    _socket.ConnectAsync(e);
                    break;
            }
        }

        #region ToDelete

        protected override void CloseConnection(SocketAsyncEventArgs e)
        {
            throw new NotImplementedException();
        }


        public override void Start()
        {
            throw new NotImplementedException();
        }

        protected override void ProcessReceivedMessageRequest(SocketAsyncEventArgs e, RequestMessage message)
        {
            throw new NotImplementedException();
        }

        protected override void ProcessReceivedMessageResponse(SocketAsyncEventArgs e, ResponseMessage message)
        {
            throw new NotImplementedException();
        }

        #endregion ToDelete
    }
}
