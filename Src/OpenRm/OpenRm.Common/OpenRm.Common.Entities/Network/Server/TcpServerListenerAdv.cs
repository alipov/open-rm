using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using OpenRm.Common.Entities.Network.Messages;

namespace OpenRm.Common.Entities.Network.Server
{
    public class TcpServerListenerAdv : TcpBase, IMessageServer
    {
        // holds maximum number of connections, defined in program
        private readonly int _maxNumConnections;
        // separate read and write operations - allocate separate Args
        private const int OpsToPreAlloc = 2;
        // pool of reusable SocketAsyncEventArgs objects for write and read socket operations
        readonly SocketAsyncEventArgsPool _argsReadWritePool;
        // represents a large reusable set of buffers for all socket operations
        private readonly BufferManager _bufferManager;
        // the socket used to listen for incoming connection requests
        private Socket _listenSocket;
        // limit number of clients
        private readonly Semaphore _maxNumberConnectedClients;

        private readonly int _port;

        public TcpServerListenerAdv(int port, int maxNumConnections, int receiveBufferSize)
            : base(receiveBufferSize)
        {
            _maxNumConnections = maxNumConnections;

            // allocate buffers such that the maximum number of sockets can have one outstanding read and 
            // write posted to the socket simultaneously  
            _bufferManager = new BufferManager(receiveBufferSize * maxNumConnections * OpsToPreAlloc, receiveBufferSize);
            // allocate pool of SocketAsyncEventArgs for sending and recieveing Args objects 
            _argsReadWritePool = new SocketAsyncEventArgsPool(maxNumConnections * OpsToPreAlloc);
            // set limiter of connected clients' number
            _maxNumberConnectedClients = new Semaphore(maxNumConnections, maxNumConnections);

            _port = port;

            //allocate resources
            Init();
        }

        // Initializes the server by preallocating reusable buffers and context objects. 
        private void Init()
        {
            // Allocates one large byte buffer which all I/O operations use a piece of.  This gaurds 
            // against memory fragmentation
            _bufferManager.InitBuffer();

            // preallocate pool of SocketAsyncEventArgs objects
            for (int i = 0; i < _maxNumConnections * OpsToPreAlloc; i++)
            {
                //Pre-allocate a set of reusable SocketAsyncEventArgs
                SocketAsyncEventArgs readWriteArg = new SocketAsyncEventArgs();
                readWriteArg.Completed += OnSendReceiveCompleted;

                // assign a byte buffer from the buffer pool to the SocketAsyncEventArg objects
                _bufferManager.SetBuffer(readWriteArg);

                // add SocketAsyncEventArg to the pool
                _argsReadWritePool.Push(readWriteArg);
            }

        }

        public void Start(Action<HostCustomEventArgs> receiveCallback)
        {
            var localEndPoint = new IPEndPoint(IPAddress.Any, _port);

            // create the socket which listens for incoming connections
            _listenSocket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _listenSocket.Bind(localEndPoint);
            // start the server with a listen backlog of 100 connections
            _listenSocket.Listen(100);
            Logger.WriteStr(string.Format("TCP server started. Listening on {0}:{1}",
                                                localEndPoint.Address, localEndPoint.Port));

            // post accepts on the listening socket
            StartAccept(null, receiveCallback);
        }

        private void StartAccept(SocketAsyncEventArgs acceptEventArg, Action<HostCustomEventArgs> receiveCallback)
        {
            if (acceptEventArg == null)
            {
                // We need to create Accept SocketAsyncEventArgs object on first run of StartAccept
                acceptEventArg = new SocketAsyncEventArgs();
                acceptEventArg.Completed += OnAcceptAsyncCompleted;
                acceptEventArg.UserToken = receiveCallback;
            }
            else
            {
                // Accept SocketAsyncEventArgs object will be reused so socket must be cleared 
                acceptEventArg.AcceptSocket = null;
            }

            // The count on a semaphore is decremented each time a thread enters the semaphore, 
            // and incremented when a thread releases the semaphore. When the count is zero, subsequent 
            // requests block until other threads release the semaphore.
            _maxNumberConnectedClients.WaitOne();

            bool willRaiseEvent = _listenSocket.AcceptAsync(acceptEventArg);
            if (!willRaiseEvent)
            {
                ProcessAccept(acceptEventArg);
            }
        }

        // This method is the callback method associated with Socket.AcceptAsync operations and is invoked when an accept operation is complete
        private void OnAcceptAsyncCompleted(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        // Transfers AcceptArgs to another SocketAsyncEventArgs object from the pool, and begins to receive data.
        private void ProcessAccept(SocketAsyncEventArgs args)
        {
            if (args.SocketError != SocketError.Success)
            {
                Logger.WriteStr("Error while processing accept.");
                StartAccept(null, args.UserToken as Action<HostCustomEventArgs>);      //start new Accept socket
                return;
            }

            Logger.WriteStr("Client connection accepted. Processing Accept from client " + args.AcceptSocket.RemoteEndPoint.ToString());

            // Get Arg from the Pool, for recieving
            SocketAsyncEventArgs readEventArgs = _argsReadWritePool.Pop();
            if (readEventArgs != null)
            {
                // create user's token
                var token = new HostAsyncUserToken();
                readEventArgs.UserToken = token;
                token.Callback = args.UserToken as Action<HostCustomEventArgs>;

                // store this object in token for fast reusing
                token.readEventArgs = readEventArgs;

                // Get the socket for the accepted client connection and put it into the readEventArg object user token
                token.Socket = args.AcceptSocket;
                readEventArgs.AcceptSocket = args.AcceptSocket;

                // Get another Arg from the Pool, for sending.
                SocketAsyncEventArgs writeEventArgs = _argsReadWritePool.Pop();

                if (writeEventArgs != null)
                {
                    // point it's UserToken to the same token
                    writeEventArgs.UserToken = token;
                    writeEventArgs.AcceptSocket = args.AcceptSocket;

                    // Link writeEventArgs to readEventArgs in the token
                    token.writeEventArgs = writeEventArgs;

                    // Initialize agent's data/info holder
                    token.Agent.Data = new ClientData();

                    // As soon as the client is connected, post a receive to the connection, to get client's identification info
                    WaitForReceiveMessage(readEventArgs);
                }
                else
                {
                    Logger.WriteStr("There is no more SocketAsyncEventArgs available in write pool! Cannot continue. Close connection.");
                    CloseConnection(args);
                }
            }
            else
            {
                Logger.WriteStr("There is no more SocketAsyncEventArgs available in read pool! Cannot continue. Close connection.");
                // increase maxNumberConnectedClients simaphore back because it has been decreased in StartAccept
                _maxNumberConnectedClients.Release();
            }

            // Accept the next connection request. We'll reuse Accept SocketAsyncEventArgs object.
            args.AcceptSocket = null;
            StartAccept(args, args.UserToken as Action<HostCustomEventArgs>);
        }

        // This method is called whenever a receive or send opreation is completed on a socket 
        private void OnSendReceiveCompleted(object sender, SocketAsyncEventArgs e)
        {
            // determine which type of operation just completed and call the associated handler
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
                default:
                    throw new ArgumentException("The last operation completed on the socket was not a receive or send");
            }
        }

        protected override void ProcessSendFailure(SocketAsyncEventArgs args)
        {
            if (((HostAsyncUserToken)args.UserToken).Callback != null)
            {
                ((HostAsyncUserToken)args.UserToken).Callback.Invoke
                                (new HostCustomEventArgs(args.SocketError, null));
            }
        }

        //protected override void ProcessReceive(SocketAsyncEventArgs e)
        //{
        //    var token = (AsyncUserTokenBase)e.UserToken;

        //    ProcessReceive(e, token);
        //}

        protected override void ProcessReceiveFailed(SocketAsyncEventArgs e)
        {
            var token = (HostAsyncUserToken)e.UserToken;

            var args = new HostCustomEventArgs(SocketError.Fault, null)
            {
                Token = token
            };

            if (token.Callback != null)
                token.Callback.Invoke(args);
        }

        protected override void ProcessReceivedMessage(SocketAsyncEventArgs e)
        {
            var token = (HostAsyncUserToken)e.UserToken;

            var message = WoxalizerAdapter.DeserializeFromXml(token.RecievedMsgData);
            var args = new HostCustomEventArgs(e.SocketError, message)
            {
                Token = token
            };
            if (token.Callback != null)
                token.Callback.Invoke(args);
        }

        public void Send(Message message, HostAsyncUserToken token)
        {
            var messageToSend = WoxalizerAdapter.SerializeToXml(message);

            // do not let sending simultaniously using the same Args object 
            token.writeSemaphore.WaitOne();

            Logger.WriteStr("Going to send message: " + Encoding.UTF8.GetString(messageToSend));

            // reset token's buffers and counters before reusing the token
            //token.Clean();

            // prepare data to send: add prefix that holds length of message
            Byte[] prefixToAdd = BitConverter.GetBytes(messageToSend.Length);

            // prepare complete data and store it into token
            token.SendingMsg = new Byte[MsgPrefixLength + messageToSend.Length];
            prefixToAdd.CopyTo(token.SendingMsg, 0);
            messageToSend.CopyTo(token.SendingMsg, MsgPrefixLength);

            StartSend(token.writeEventArgs);
        }

        protected override void CloseConnection(SocketAsyncEventArgs e)
        {
            var token = (HostAsyncUserToken)e.UserToken;
            String ClientEP = token.Socket.RemoteEndPoint.ToString();   //get name before we close the connection

            // close the socket associated with the client
            try
            {
                token.Socket.Shutdown(SocketShutdown.Send);
            }
            // throws if client process has already closed
            catch (Exception) { }
            token.Socket.Close();

            Logger.WriteStr("A client " + ClientEP + " has been disconnected from the server.");

            // Free the SocketAsyncEventArgs so they can be reused by another client, and return them back to pool
            var readArgs = token.readEventArgs;
            var writeArgs = token.writeEventArgs;
            if (readArgs != null)
            {
                readArgs.UserToken = null;
                _argsReadWritePool.Push(readArgs);
            }
            if (writeArgs != null)
            {
                writeArgs.UserToken = null;
                _argsReadWritePool.Push(writeArgs);
            }

            // decrease number of connected clients
            _maxNumberConnectedClients.Release();
        }

        #region ToDelete


        // Invoked when an asycnhronous receive operation completes.  
        // If the remote host closed the connection, then the socket is closed.
        //protected new void ProcessReceive(SocketAsyncEventArgs e)
        //{
        //    var token = (HostAsyncUserToken)e.UserToken;

        //    // Check if the remote host closed the connection
        //    //  (SocketAsyncEventArgs.BytesTransferred is the number of bytes transferred in the socket operation.)
        //    if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
        //    {
        //        // got message. need to handle it
        //        Logger.WriteStr("Recieved data (" + e.BytesTransferred + " bytes)");

        //        // now we need to check if we have complete message in our recieved data.
        //        // if yes - process it
        //        // but few messages can be combined into one send/recieve operation,
        //        //  or we can get only half message or just part of Prefix 
        //        //  if we get part of message, we'll hold it's data in UserToken and use it on next Receive

        //        int i = 0;      // go through buffer of currently received data 
        //        while (i < e.BytesTransferred)
        //        {
        //            // Determine how many bytes we want to transfer to the buffer and transfer them 
        //            int bytesAvailable = e.BytesTransferred - i;
        //            if (token.RecievedMsgData == null)
        //            {
        //                // token.msgData is empty so we a dealing with Prefix.
        //                // Copy the incoming bytes into token's prefix's buffer
        //                // All incoming data is in e.Buffer, at e.Offset position
        //                int bytesRequested = MsgPrefixLength - token.RecievedPrefixPartLength;
        //                int bytesTransferred = Math.Min(bytesRequested, bytesAvailable);
        //                Array.Copy(e.Buffer, e.Offset + i, token.PrefixData, token.RecievedPrefixPartLength, bytesTransferred);
        //                i += bytesTransferred;

        //                token.RecievedPrefixPartLength += bytesTransferred;

        //                if (token.RecievedPrefixPartLength != MsgPrefixLength)
        //                {
        //                    // We haven't gotten all the prefix buffer yet: call Receive again.
        //                    Logger.WriteStr("We've got just a part of prefix. Waiting for more data to arrive...");
        //                    StartReceive(e);
        //                    return;
        //                }
        //                // We've gotten the prefix buffer 
        //                int length = BitConverter.ToInt32(token.PrefixData, 0);
        //                Logger.WriteStr(" Got prefix representing value: " + length);

        //                if (length == 0)
        //                {
        //                    Logger.WriteStr("We've got keep-alive / empty message");
        //                    //TODO: remove Sleep:
        //                    //Thread.Sleep(10000);
        //                    break;  //exit while
        //                }

        //                if (length < 0)
        //                    throw new ProtocolViolationException("Invalid message prefix");

        //                // Save prefix value into token
        //                token.MessageLength = length;

        //                // Create the data buffer and start reading into it 
        //                token.RecievedMsgData = new byte[length];

        //                // zero prefix counter
        //                //token.RecievedPrefixPartLength = 0;
        //            }
        //            else
        //            {
        //                // We're reading into the data buffer  
        //                int bytesRequested = token.MessageLength - token.RecievedMsgPartLength;
        //                int bytesTransferred = Math.Min(bytesRequested, bytesAvailable);
        //                Array.Copy(e.Buffer, e.Offset + i, token.RecievedMsgData, token.RecievedMsgPartLength, bytesTransferred);
        //                Logger.WriteStr("message till now: " + Encoding.UTF8.GetString(token.RecievedMsgData));
        //                i += bytesTransferred;

        //                token.RecievedMsgPartLength += bytesTransferred;
        //                if (token.RecievedMsgPartLength != token.RecievedMsgData.Length)
        //                {
        //                    // We haven't gotten all the data buffer yet: call Receive again to get more data
        //                    StartReceive(e);
        //                    return;
        //                }
        //                // we've gotten an entire packet
        //                Logger.WriteStr("Got complete message from " + token.Socket.RemoteEndPoint + ": " + Encoding.ASCII.GetString(token.RecievedMsgData));


        //                //ProcessReceivedMessage(e);
        //                var message = WoxalizerAdapter.DeserializeFromXml(token.RecievedMsgData);
        //                var args = new HostCustomEventArgs(e.SocketError, message)
        //                               {
        //                                   Token = token
        //                               };
        //                if(token.Callback != null)
        //                    token.Callback.Invoke(args);

        //                // Clean token's recieve buffer
        //                token.CleanForRecieve();

        //                // wait for next message
        //                StartReceive(e);


        //            }
        //        }   //end while

        //        //release 
        //    }
        //    else
        //    {
        //        Logger.WriteStr("ERROR: Failed to get data on socket " + token.Socket.LocalEndPoint + " due to exception:\n"
        //            + new SocketException((int)e.SocketError) + "\n"
        //            + "Closing this connection....");
        //        CloseConnection(e);
        //    }
        //}

        #endregion
    }
}
