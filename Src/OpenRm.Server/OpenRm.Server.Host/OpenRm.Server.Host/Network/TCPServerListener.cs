using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace OpenRm.Server.Host
{
    class TCPServerListener
    {
        int maxNumConnections;              // holds maximum number of connections, defined in program
        int msgPrefixLength = 4;            // message prefix length (4 bytes). Prefix added to each message: it holds sent message's length
        const int opsToPreAlloc = 2;        // read, write (don't alloc buffer space for accepts)
        BufferManager bufferManager;        // represents a large reusable set of buffers for all socket operations
        Socket listenSocket;                // the socket used to listen for incoming connection requests
        SocketAsyncEventArgsPool argsReadWritePool;     // pool of reusable SocketAsyncEventArgs objects for write, read and accept socket operations
        Semaphore maxNumberAcceptedClients;             // limit number of clients
        Logger log;                                     // log file writer

        public TCPServerListener(int port, int maxNumConnections, int receiveBufferSize, Logger log)
        {
            this.maxNumConnections = maxNumConnections;
            this.log = log;

            // allocate buffers such that the maximum number of sockets can have one outstanding read and 
            //write posted to the socket simultaneously  
            bufferManager = new BufferManager(receiveBufferSize * maxNumConnections * opsToPreAlloc, receiveBufferSize);

            argsReadWritePool = new SocketAsyncEventArgsPool(maxNumConnections);
            maxNumberAcceptedClients = new Semaphore(maxNumConnections, maxNumConnections); 

            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, port);

            Init();
            log.WriteStr("Init level completed");
            Start(localEndPoint);
        }

        // Initializes the server by preallocating reusable buffers and context objects. 
        public void Init()
        {
            // Allocates one large byte buffer which all I/O operations use a piece of.  This gaurds 
            // against memory fragmentation
            bufferManager.InitBuffer();

            // preallocate pool of SocketAsyncEventArgs objects
            SocketAsyncEventArgs readWriteArg;

            for (int i = 0; i < maxNumConnections; i++)
            {
                //Pre-allocate a set of reusable SocketAsyncEventArgs
                readWriteArg = new SocketAsyncEventArgs();
                readWriteArg.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                readWriteArg.UserToken = new AsyncUserToken();

                // assign a byte buffer from the buffer pool to the SocketAsyncEventArg object
                bufferManager.SetBuffer(readWriteArg);

                // add SocketAsyncEventArg to the pool
                argsReadWritePool.Push(readWriteArg);
            }

        }

        // Starts the server such that it is listening for incoming connection requests.    
        public void Start(IPEndPoint localEndPoint)
        {
            // create the socket which listens for incoming connections
            listenSocket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(localEndPoint);
            // start the server with a listen backlog of 100 connections
            listenSocket.Listen(100);
            log.WriteStr("Listening on " + localEndPoint.Address + ":" + localEndPoint.Port);

            // post accepts on the listening socket
            StartAccept(null);

            log.WriteStr("TCP terminated.");
        }

        // Begins an operation to accept a connection request from the client 
        // Recieves context object to use when issuing the accept operation on the server's listening socket
        public void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            if (acceptEventArg == null)     
            {
                // We need to create Accept SocketAsyncEventArgs object on first run of StartAccept
                acceptEventArg = new SocketAsyncEventArgs();
                acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptEventArg_Completed);
            }
            else
            {
                // Accept SocketAsyncEventArgs object will be reused so socket must be cleared 
                acceptEventArg.AcceptSocket = null;
            }

            maxNumberAcceptedClients.WaitOne();
            bool willRaiseEvent = listenSocket.AcceptAsync(acceptEventArg);
            if (!willRaiseEvent)
            {
                ProcessAccept(acceptEventArg);
            }
        }

        // This method is the callback method associated with Socket.AcceptAsync operations and is invoked when an accept operation is complete
        void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            log.WriteStr("Client connection accepted. Processing Accept...");

            // Get the socket for the accepted client connection and put it into the ReadEventArg object user token
            SocketAsyncEventArgs readEventArgs = argsReadWritePool.Pop();
            ((AsyncUserToken)readEventArgs.UserToken).Socket = e.AcceptSocket;

            // As soon as the client is connected, post a receive to the connection
            bool willRaiseEvent = e.AcceptSocket.ReceiveAsync(readEventArgs);
            if (!willRaiseEvent)
            {
                // need to be proceeded synchroniously
                ProcessReceive(readEventArgs);
            }

            // Accept the next connection request. We'll reuse Accept SocketAsyncEventArgs object.
            StartAccept(e);
        }

        // This method is called whenever a receive or send opreation is completed on a socket 
        void IO_Completed(object sender, SocketAsyncEventArgs e)
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


        // Invoked when an asycnhronous receive operation completes.  
        // If the remote host closed the connection, then the socket is closed.
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            // check if the remote host closed the connection
            AsyncUserToken token = (AsyncUserToken)e.UserToken;
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                log.WriteStr("Recieved data on socket " + token.socket.LocalEndPoint.ToString());

                // now we need to check if we got complete message.
                // if not - call recieve method untill we get a whole message
                // but first, we need to recieve Prefix to know how much bytes are in our message:
                if (token.recievedPrefixPart == 0)
                    token.prefixData = new Byte[msgPrefixLength];
                if (token.recievedPrefixPart < msgPrefixLength)
                {
                    int bytesToCopyToPrefix;
                    if (e.BytesTransferred >= msgPrefixLength)
                    {
                        bytesToCopyToPrefix = msgPrefixLength - token.recievedPrefixPart;
                    }
                    else
                    {
                        bytesToCopyToPrefix = e.BytesTransferred - token.recievedPrefixPart;
                    }
                    // 0??????
                    Buffer.BlockCopy(e.Buffer, 0, token.prefixData, token.recievedPrefixPart, bytesToCopyToPrefix);
                    //now copy remainig message data 

                        

                }
                else
                {
                    //...


                    token.recievedPrefixPart = 0; //reset for next message
                }






                //echo the data received back to the client
                bool willRaiseEvent = token.Socket.SendAsync(e);
                if (!willRaiseEvent)
                {
                    ProcessSend(e);
                }

            }
            else
            {
                log.WriteStr("ERROR: Failed to get data on socket " + token.socket.LocalEndPoint.ToString() + ". Closing this connection.");
                CloseClientSocket(e);
            }
        }


        // This method is invoked when an asynchronous send operation completes.
        // The method issues another receive on the socket to read any additional data sent from the client.
        private void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                // done echoing data back to the client
                AsyncUserToken token = (AsyncUserToken)e.UserToken;
                // read the next block of data send from the client
                bool willRaiseEvent = token.Socket.ReceiveAsync(e);
                if (!willRaiseEvent)
                {
                    ProcessReceive(e);
                }
            }
            else
            {
                CloseClientSocket(e);
            }
        }

        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = e.UserToken as AsyncUserToken;
            String clientEP = token.Socket.RemoteEndPoint.ToString();

            // close the socket associated with the client
            try
            {
                token.Socket.Shutdown(SocketShutdown.Send);
            }
            // throws if client process has already closed
            catch (Exception) { }
            token.Socket.Close();
            maxNumberAcceptedClients.Release();

            log.WriteStr("A client " + clientEP + " has been disconnected from the server.");

            // Free the SocketAsyncEventArg so they can be reused by another client
            argsReadWritePool.Push(e);
        }

    }
}
