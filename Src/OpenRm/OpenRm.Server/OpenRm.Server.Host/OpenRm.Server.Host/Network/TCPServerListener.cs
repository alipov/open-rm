using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Xml;
using OpenRm.Common.Entities;
using Woxalizer;

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
        //Logger log;                                     // log file writer

        public TCPServerListener(int port, int maxNumConnections, int receiveBufferSize)
        {
            this.maxNumConnections = maxNumConnections;
            this.receiveBufferSize = receiveBufferSize;
            //this.log = log;

            // allocate buffers such that the maximum number of sockets can have one outstanding read and 
            // write posted to the socket simultaneously  
            bufferManager = new BufferManager(receiveBufferSize * maxNumConnections * opsToPreAlloc, receiveBufferSize);

            argsReadWritePool = new SocketAsyncEventArgsPool(maxNumConnections);
            maxNumberAcceptedClients = new Semaphore(maxNumConnections, maxNumConnections); 

            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, port);

            Init();
            Logger.WriteStr("Init level completed");
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
            Logger.WriteStr("TCP server started. Listening on " + localEndPoint.Address + ":" + localEndPoint.Port);

            // post accepts on the listening socket
            StartAccept(null);

            // Pause here 
            Console.WriteLine("Press any key to terminate the server process....");
            Console.ReadKey();
            Logger.WriteStr("TCP terminated.");
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

        // Transfers AcceptArgs to another SocketAsyncEventArgs object from the pool, and begins to receive data.
        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                Logger.WriteStr("Error while processing accept.");
                StartAccept(null);      //start new Accept socket
                return;
            }

            //TODO: deleteme
            //StartAccept(null);

            Logger.WriteStr("Client connection accepted. Processing Accept from client " + e.AcceptSocket.RemoteEndPoint.ToString());

            // Get the socket for the accepted client connection and put it into the ReadEventArg object user token
            SocketAsyncEventArgs readEventArgs = argsReadWritePool.Pop();
            if (readEventArgs != null)
            {
                ((AsyncUserToken)readEventArgs.UserToken).socket = e.AcceptSocket;
                readEventArgs.AcceptSocket = e.AcceptSocket;
                e.AcceptSocket = null;                          // We'll reuse Accept SocketAsyncEventArgs object

                // As soon as the client is connected, post a receive to the connection, to get client's identification info
                StartReceive(readEventArgs);

            }
            else
            {
                Logger.WriteStr("There is no more SocketAsyncEventArgs available in r/w pool");
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
            Logger.WriteStr("StartReceive has been run");
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
                            // We haven't gotten all the prefix buffer yet: call Receive again.
                            Logger.WriteStr("We've got just a part of prefix. Waiting for more data to arrive...");
                            StartReceive(e);
                            return;
                        }
                        else
                        {
                            // We've gotten the prefix buffer 
                            int length = BitConverter.ToInt32(token.prefixData, 0);
                            Logger.WriteStr(" Got prefix representing value: " + length);

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
                        Logger.WriteStr("message till now: " + Encoding.ASCII.GetString(token.msgData));
                        i += bytesTransferred;

                        token.recievedMsgPartLength += bytesTransferred;
                        if (token.recievedMsgPartLength != token.msgData.Length)
                        {
                            // We haven't gotten all the data buffer yet: call Receive again to get more data
                            StartReceive(e);
                            return;
                        }
                        else
                        {
                            // we've gotten an entire packet
                            Logger.WriteStr("Got complete message from " + token.socket.RemoteEndPoint.ToString() + ": " + Encoding.ASCII.GetString(token.msgData));
// TODO:  get token.msgData data and convert to XML, .... . . . ..

                            ProcessReceivedMessage(e);

                            // empty Token's buffers and counters
                            token.Clean();

                            //TODO:  remove sending this data (it's for testing here)
                            var msg = new ResponseMessage();
                            msg.Response = new IpConfigData();
                            msg.OperationType = 333;
                            SendMessage(e, SerializeToXml(msg));

                        } 
                    }
                } 
            }
            else
            {
                Logger.WriteStr("ERROR: Failed to get data on socket " + token.socket.LocalEndPoint.ToString() + " due to exception:\n"
                    + new SocketException((int)e.SocketError).ToString() + "\n"
                    + "Closing this connection....");
                CloseClientSocket(e);
            }
        }


        // Prepares data to be sent and calls sending method. 
        // It can process large messages by cutting them into small ones.
        // The method issues another receive on the socket to read client's answer.
        private void SendMessage(SocketAsyncEventArgs e, Byte[] msgToSend)
        {
            //TODO:  maybe remove 3-byte descriptor from beginning of array?
            Logger.WriteStr("Going to send message: " + Encoding.UTF8.GetString(msgToSend));

            AsyncUserToken token = (AsyncUserToken)e.UserToken;

            // prepare data to send: add prefix that holds length of message
            Byte[] prefixToAdd = BitConverter.GetBytes(msgToSend.Length);
            ////if (prefixToAdd.Length != msgPrefixLength )
            ////{
            ////    //TODO:  Do we need this check??? if yes - throw Exception
            ////    Logger.WriteStr("ERROR: prefixToAdd.Length is not the same size of msgPrefixLength! Check your OS platform...");
            ////    return;
            ////}

            // prepare complete data and store it into token
            token.sendingMsg = new Byte[msgPrefixLength + msgToSend.Length];
            prefixToAdd.CopyTo(token.sendingMsg, 0);
            msgToSend.CopyTo(token.sendingMsg, msgPrefixLength);

            StartSend(e);
 
        }


        private void StartSend(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = (AsyncUserToken)e.UserToken;

            int bytesToTransfer = Math.Min(receiveBufferSize, token.sendingMsg.Length - token.sendingMsgBytesSent);
            Array.Copy(token.sendingMsg, token.sendingMsgBytesSent, e.Buffer, e.Offset, bytesToTransfer);
            e.SetBuffer(e.Offset, bytesToTransfer);

            bool willRaiseEvent = e.AcceptSocket.SendAsync(e);
            if (!willRaiseEvent)
            {
                ProcessSend(e);
            }
        }


        // This method is invoked when an asynchronous send operation completes.
        private void ProcessSend(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = (AsyncUserToken)e.UserToken;

            if (e.SocketError == SocketError.Success)
            {    
                token.sendingMsgBytesSent += receiveBufferSize;     // receiveBufferSize is the maximum data length in one send
                if (token.sendingMsgBytesSent < token.sendingMsg.Length)
                {
                    // Not all message has been sent, so send next part
                    Logger.WriteStr(token.sendingMsgBytesSent + " of " + token.sendingMsg.Length + " have been sent. Calling additional Send...");
                    StartSend(e);
                    return;
                }

                Logger.WriteStr(" Message has been sent.");

                // reset token's buffers and counters before reusing the token
                token.Clean();

                // read the answer send from the client
                StartReceive(e);

            }
            else
            {
                Logger.WriteStr(" Message has failed to be sent.");
                token.Clean();
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

            Logger.WriteStr("A client " + clientEP + " has been disconnected from the server.");

            // Free the SocketAsyncEventArg so they can be reused by another client
            argsReadWritePool.Push(e);
        }


        private void ProcessReceivedMessage(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = (AsyncUserToken)e.UserToken;
            
            Message message = DeserializeFromXml(token.msgData);

            // TODO: why do not write: "if (message is RequestMessage)" ...  - so we don't need MessageType!
            switch ((EMessageType)message.MessageType)
            {
                case EMessageType.Request:
                    ProcessReceivedMessageRequest(e, (RequestMessage)message);
                    break;
                case EMessageType.Response:
                    ProcessReceivedMessageResponse(e, (ResponseMessage)message);
                    break;
                default:
                    throw new ApplicationException();
            }
        }

        private void ProcessReceivedMessageRequest(SocketAsyncEventArgs e, RequestMessage message)
        {
            //server recieves requests only from Console!
        }

        private void ProcessReceivedMessageResponse(SocketAsyncEventArgs e, ResponseMessage message)
        {
            AsyncUserToken token = (AsyncUserToken)e.UserToken;

            if (message.Response is IdentificationData)
            {
                IdentificationData idata = (IdentificationData) message.Response;
                Logger.WriteStr(" * New client has connected: " + idata.deviceName);
                // ...create ClientData and add to token
                //...
            }
            else if (message.Response is IpConfigData)
            {
                //...
            }
            else
            {
                Logger.WriteStr(" Recieved unkown response! ");
            }
                
        }


        // TODO:  move to another class?
        private static Byte[] SerializeToXml(ResponseMessage msg)
        {
            var mem = new MemoryStream();
            var writer = XmlWriter.Create(mem);

            using (var woxalizer = new WoxalizerUtil(AssemblyResolveHandler))
            {
                woxalizer.Save(msg, writer);
            }
            return mem.ToArray();
        }

        private static Message DeserializeFromXml(Byte[] msg)
        {
            Message message;
            var mem = new MemoryStream(msg);
            var reader = XmlReader.Create(mem);

            using (var woxalizer = new WoxalizerUtil(AssemblyResolveHandler))
            {
                // TODO:  resolve PROBLEM
                message = (Message)woxalizer.Load(reader);
            }
            return message;
        }



        static Assembly AssemblyResolveHandler(object sender, ResolveEventArgs args)
        {
            //This handler is called only when the common language runtime tries to bind to the assembly and fails.

            //Retrieve the list of referenced assemblies in an array of AssemblyName.
            var assemblyPath = string.Empty;

            Assembly objExecutingAssemblies = Assembly.GetExecutingAssembly();
            AssemblyName[] arrReferencedAssmbNames = objExecutingAssemblies.GetReferencedAssemblies();

            //Loop through the array of referenced assembly names.
            foreach (AssemblyName strAssmbName in arrReferencedAssmbNames)
            {
                var requestedAssembly = args.Name.Substring(0, args.Name.IndexOf(","));

                //Check for the assembly names that have raised the "AssemblyResolve" event.
                if (strAssmbName.FullName.Substring(0, strAssmbName.FullName.IndexOf(",")) == requestedAssembly)
                {
                    //Build the path of the assembly from where it has to be loaded.
                    var rootDirecotory = Directory.GetParent(Directory.GetCurrentDirectory());
                    assemblyPath = Directory.GetFiles
                                    (rootDirecotory.FullName, requestedAssembly + ".dll", SearchOption.AllDirectories).Single();
                    break;
                    //assemblyPath = Path.Combine(rootDirecotory.FullName, "Common", requestedAssembly + ".dll");
                }
            }
            //Load the assembly from the specified path.
            Assembly myAssembly = null;

            // failing to ignore queries for satellite resource assemblies or using [assembly: NeutralResourcesLanguage("en-US", UltimateResourceFallbackLocation.MainAssembly)] 
            // in AssemblyInfo.cs will crash the program on non en-US based system cultures.
            if (!string.IsNullOrWhiteSpace(assemblyPath))
                myAssembly = Assembly.LoadFrom(assemblyPath);

            //Return the loaded assembly.
            return myAssembly;
        }

    }
}
