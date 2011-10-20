﻿using System;
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
        public const int msgPrefixLength = 4;            // message prefix length (4 bytes). Prefix added to each message: it holds sent message's length
        public int maxNumConnections;              // holds maximum number of connections, defined in program
        public int receiveBufferSize;
        public const int opsToPreAlloc = 2;        // read, write (don't alloc buffer space for accepts)
        BufferManager bufferManager;        // represents a large reusable set of buffers for all socket operations
        Socket listenSocket;                // the socket used to listen for incoming connection requests
        SocketAsyncEventArgsPool argsReadWritePool;     // pool of reusable SocketAsyncEventArgs objects for write, read and accept socket operations
        Semaphore maxNumberAcceptedClients;             // limit number of clients
        Logger log;                                     // log file writer

        public TCPServerListener(int port, int maxNumConnections, int receiveBufferSize, Logger log)
        {
            this.maxNumConnections = maxNumConnections;
            this.receiveBufferSize = receiveBufferSize;
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
            log.WriteStr("TCP server started. Listening on " + localEndPoint.Address + ":" + localEndPoint.Port);

            // post accepts on the listening socket
            StartAccept(null);

            // Pause here 
            Console.WriteLine("Press any key to terminate the server process....");
            Console.ReadKey();
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

        // Transfers Accept to another SocketAsyncEventArgs object from the pool, and begins to receive data.
        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                log.WriteStr("Error while processing accept.");
                StartAccept(null);      //start new Accept socket
                return;
            }

            //TODO: deleteme
            //StartAccept(null);

            log.WriteStr("Client connection accepted. Processing Accept from client " + e.AcceptSocket.RemoteEndPoint.ToString());

            // Get the socket for the accepted client connection and put it into the ReadEventArg object user token
            SocketAsyncEventArgs readEventArgs = argsReadWritePool.Pop();
            if (readEventArgs != null)
            {
                ((AsyncUserToken)readEventArgs.UserToken).socket = e.AcceptSocket;
                readEventArgs.AcceptSocket = e.AcceptSocket;
                e.AcceptSocket = null;                          // We'll reuse Accept SocketAsyncEventArgs object

                //TODO: delete??
                // As soon as the client is connected, ask him to authorize
                //SendMessage(readEventArgs, "hello!");


                // As soon as the client is connected, post a receive to the connection
                StartReceive(readEventArgs);
                ////// As soon as the client is connected, post a receive to the connection
                ////bool willRaiseEvent = e.AcceptSocket.ReceiveAsync(readEventArgs);
                ////if (!willRaiseEvent)
                ////{
                ////    ProcessReceive(readEventArgs);
                ////}
            }
            else
            {
                log.WriteStr("There is no more SocketAsyncEventArgs available in r/w pool");
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


        private void StartReceive(SocketAsyncEventArgs readEventArgs)
        {
            readEventArgs.SetBuffer(readEventArgs.Offset, receiveBufferSize);
            bool willRaiseEvent = readEventArgs.AcceptSocket.ReceiveAsync(readEventArgs);
            if (!willRaiseEvent)
            {
                // need to be proceeded synchroniously
                ProcessReceive(readEventArgs);
            }
            log.WriteStr("StartReceive has been run");
        }

        // Invoked when an asycnhronous receive operation completes.  
        // If the remote host closed the connection, then the socket is closed.
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = (AsyncUserToken)e.UserToken;
            // Check if the remote host closed the connection
            //  (SocketAsyncEventArgs.BytesTransferred is the number of bytes transferred in the socket operation.)
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)         
            {
                // got message. need to handle it
                log.WriteStr("Recieved data (" + e.BytesTransferred + " bytes)");

                // now we need to check if we have complete message in our recieved data.
                // if yes - process it
                // but few messages can be combined into one send/recieve operation,
                //  or we can get only half message or just part of Prefix 
                //  if we get part of message, we'll hold it's data in UserToken and use it on next Receive

                int i = 0;      // go through buffer of currently received data 
                while (i != e.BytesTransferred)
                {
                    // Determine how many bytes we want to transfer to the buffer and transfer them 
                    int bytesAvailable = e.BytesTransferred - i;
                    if (token.msgData == null)
                    {
                        // token.msgData is empty so we a dealing with Prefix.
                        // Copy the incoming bytes into token's prefix's buffer
                        // All incoming data is in e.Buffer, at e.Offset position
                        int bytesRequested = msgPrefixLength - token.recievedPrefixPartLength;
                        int bytesTransferred = Math.Min(bytesRequested, bytesAvailable);
                        Array.Copy(e.Buffer, e.Offset + i, token.prefixData, token.recievedPrefixPartLength, bytesTransferred);
                        i += bytesTransferred;

                        token.recievedPrefixPartLength += bytesTransferred;

                        if (token.recievedPrefixPartLength != msgPrefixLength)
                        {
                            // We haven't gotten all the prefix buffer yet: just wait for more data to arrive
                            log.WriteStr("We've got just a part of prefix. Waiting for more data to arrive...");
// TODO:  maybe need to call Receive() again or just wait?
                            //////token.i = i;
                            //StartReceive(e);
                            return;
                        }
                        else
                        {
                            // We've gotten the length buffer 
                            int length = BitConverter.ToInt32(token.prefixData, 0);
                            log.WriteStr(" Got prefix representing value: " + length);

                            if (length < 0)
                                throw new System.Net.ProtocolViolationException("Invalid message prefix");

                            // Save prefix value into token
                            token.messageLength = length;

                            // Create the data buffer and start reading into it 
                            token.msgData = new byte[length];
                            
                            // zero prefix counter
                            token.recievedPrefixPartLength = 0;
                        }

                    }
                    else
                    {
                        // We're reading into the data buffer  
                        int bytesRequested = token.messageLength - token.recievedMsgPartLength;
                        int bytesTransferred = Math.Min(bytesRequested, bytesAvailable);
                        Array.Copy(e.Buffer, e.Offset + i, token.msgData, token.recievedMsgPartLength, bytesTransferred);
                        log.WriteStr("message till now: " + Encoding.ASCII.GetString(token.msgData));
                        i += bytesTransferred;

                        token.recievedMsgPartLength += bytesTransferred;
                        if (token.recievedMsgPartLength != token.msgData.Length)
                        {
                            // We haven't gotten all the data buffer yet: just wait for more data to arrive
// TODO:  maybe need to call Receive() again
                            ////token.i = i;
                            StartReceive(e);
                            return;
                        }
                        else
                        {
                            // we've gotten an entire packet
                            log.WriteStr("Got message from " + token.socket.RemoteEndPoint.ToString() + ": " + Encoding.ASCII.GetString(token.msgData));
// TODO:  get token.msgData data and convert to XML, .... . . . ..


                            //TODO:  remove sending this data (it's for testing here)
                            //SendMessage(e, "lalala");

                            // empty Token's buffers and counters
                            token.Clean();
                        } 

                    }

                } 


                //  TO DELETE:
                // first, we need to recieve Prefix to know how much bytes are in our message
                //if (token.recievedPrefixPartLength == 0)
                //    token.prefixData = new Byte[msgPrefixLength];   //initialize byte array for holding Prefix
                //if (token.recievedPrefixPartLength < msgPrefixLength)
                //{
                //    int bytesToCopyToPrefix;    //number of bytes that we should get to complete Prefix
                //    if (e.BytesTransferred >= msgPrefixLength)
                //    {
                //        bytesToCopyToPrefix = msgPrefixLength - token.recievedPrefixPartLength;
                //    }
                //    else
                //    {
                //        bytesToCopyToPrefix = e.BytesTransferred - token.recievedPrefixPartLength;
                //    }
                //    // copy it to token.prefixData byte array
                //    Buffer.BlockCopy(e.Buffer, token.msgStartOffset + token.recievedPrefixPartLength, token.prefixData, token.recievedPrefixPartLength, bytesToCopyToPrefix);
                //    //now copy remainig message data 
                //}
                //else
                //{
                //    //...
                //    token.recievedPrefixPart = 0; //reset for next message
                //}
                //...
                //echo the data received back to the client
                //



            }
            else
            {
                log.WriteStr("ERROR: Failed to get data on socket " + token.socket.LocalEndPoint.ToString() + ". Closing this connection.");
                CloseClientSocket(e);
            }

            log.WriteStr(" -end of ProcessRecieve");
        }

        // Performs sending messages. 
        // It can process large messages by cutting them into small ones.
        // The method issues another receive on the socket to read any additional data sent from the client.
        private void SendMessage(SocketAsyncEventArgs e, String msg)
        {
            log.WriteStr("Starting to send message: " + msg);

            AsyncUserToken token = (AsyncUserToken)e.UserToken;

            // prepare data to send: add prefix that holds length of message:
            Byte[] msgToSend = Encoding.ASCII.GetBytes(msg);
            Byte[] prefixToAdd = BitConverter.GetBytes((Int32)msgToSend.Length);
            if (prefixToAdd.Length != msgPrefixLength )
            {
                log.WriteStr("ERROR: prefixToAdd.Length is not the same size of msgPrefixLength! Check your OS platform...");
                return;
            }
            Byte[] dataToSend = new Byte[msgPrefixLength + msgToSend.Length];
            prefixToAdd.CopyTo(dataToSend, 0);
            msgToSend.CopyTo(dataToSend, msgPrefixLength);

            // our message may be bigger then token.Buffer, so we need to split it and perform few sends
            int i = 0;
            while (i != dataToSend.Length)
            {
                // copy the data into the buffer 
                int bytesToTransfer = Math.Min(receiveBufferSize, dataToSend.Length - i);
                Array.Copy(dataToSend, i, e.Buffer, e.Offset, bytesToTransfer);
                e.SetBuffer(e.Offset, bytesToTransfer);

                bool willRaiseEvent = token.socket.SendAsync(e);
                if (!willRaiseEvent)
                {
log.WriteStr("!willRaiseEvent -> ProcessSend(e)"); //TODO: remove
                    ProcessSend(e);
                }

                i += bytesToTransfer;
log.WriteStr("i = " + i); //TODO: remove
            }

            // reset token's buffers and counters before reusing the token
            token.Clean();

            // read the answer send from the client
            bool willRaiseRecieveEvent = token.socket.ReceiveAsync(e);
            if (!willRaiseRecieveEvent)
            {
log.WriteStr("!willRaiseEvent -> ProcessReceive(e)"); //TODO: remove
                ProcessReceive(e);
            }
 
        }


        // This method is invoked when an asynchronous send operation completes.
        private void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                log.WriteStr(" Message has been sent.");   
            }
            else
            {
                log.WriteStr(" Message failed to be sent.");
                CloseClientSocket(e);
            }
        }


        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = e.UserToken as AsyncUserToken;
            String clientEP = token.socket.RemoteEndPoint.ToString();

            // close the socket associated with the client
            try
            {
                token.socket.Shutdown(SocketShutdown.Send);
            }
            // throws if client process has already closed
            catch (Exception) { }
            token.socket.Close();
            maxNumberAcceptedClients.Release();
            token.Clean();

            log.WriteStr("A client " + clientEP + " has been disconnected from the server.");

            // Free the SocketAsyncEventArg so they can be reused by another client
            argsReadWritePool.Push(e);
        }

    }
}
