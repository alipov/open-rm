using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using OpenRm.Common.Entities;
using OpenRm.Common.Entities.Network;
using OpenRm.Common.Entities.Network.Messages;

namespace OpenRm.Server.Host.Network
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

            Logger.WriteStr("Client connection accepted. Processing Accept from client " + args.AcceptSocket.RemoteEndPoint);

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
                    token.Agent = new Agent();

                    // Start timer that sends keep-alive messages
                    token.KeepAliveTimer = new KeepAliveTimer(token);
                    token.KeepAliveTimer.Elapsed += SendKeepAlive;

                    // As soon as the client is connected, post a receive to the connection, to get client's identification info
                    WaitForReceiveMessage(readEventArgs);
                }
                else
                {
                    Logger.WriteStr("WARNING: There is no more SocketAsyncEventArgs available in write pool! Cannot continue. Close connection.");
                    CloseConnection(args);
                }
            }
            else
            {
                Logger.WriteStr("WARNING: There is no more SocketAsyncEventArgs available in read pool! Cannot continue. Close connection.");
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

        

        // Deserialize received message and invoke callback that should process the message
        protected override void ProcessReceivedMessage(SocketAsyncEventArgs e)
        {
            var token = (HostAsyncUserToken)e.UserToken;

            // decrypt message data
            var decryptedMsgData = EncryptionAdapter.Decrypt(token.RecievedMsgData);
            Logger.WriteStr(" Dectypted message: " + utf8.GetString(decryptedMsgData));

            // deserialize to object
            var message = WoxalizerAdapter.DeserializeFromXml(decryptedMsgData);

            var args = new HostCustomEventArgs(e.SocketError, message)
            {
                Token = token
            };
            if (token.Callback != null)
                token.Callback.Invoke(args);
        }


        // Start sending single message to client which token belongs to.
        public void Send(Message message, HostAsyncUserToken token)
        {
            byte[] messageToSend = WoxalizerAdapter.SerializeToXml(message);
            Logger.WriteStr(" The message to be send: " + utf8.GetString(messageToSend));

            // encrypt message
            byte[] encryptedMsgData = EncryptionAdapter.Encrypt(messageToSend);

            SendMessage(token, encryptedMsgData);

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

        // Processing send/receive failure
        //  ProcessSendFailure and ProcessReceiveFailure are called whenever Async operation receives SocketError,
        //  so we can assume that there was serious problem such as closed, broken or bad connection,
        //  that's why we decide to close socket, so client will have to reconnect to the server
        protected override void ProcessFailure(SocketAsyncEventArgs e)
        {
            var token = (HostAsyncUserToken)e.UserToken;
            var callback = token.Callback;
            var error = e.SocketError;

            CloseConnection(e);

            var args = new HostCustomEventArgs(error, null)
            {
                Token = token
            };

            if (callback != null)
                callback.Invoke(args);
        }


        //Close connection and free resources
        protected override void CloseConnection(SocketAsyncEventArgs e)
        {
            var token = (HostAsyncUserToken)e.UserToken;
            try
            {
                //maybe timer has been stopped already
                token.KeepAliveTimer.Stop();
            }
            catch (Exception) { }

            String clientEp = token.Socket.RemoteEndPoint.ToString();   //get name before we close the connection

            // close the socket associated with the client
            try
            {
                token.Socket.Shutdown(SocketShutdown.Send);
            }
            // throws if client process has already closed
            catch (Exception) { }
            token.Socket.Close();

            Logger.WriteStr("A client " + clientEp + " has been disconnected from the server.");

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

            token.Wipe();

            // decrease number of connected clients
            _maxNumberConnectedClients.Release();
        }

        
    }
}
