using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using OpenRm.Common.Entities.Network.Messages;

namespace OpenRm.Common.Entities.Network.Server
{
    internal class TcpServerListener : NonInterfacedClientBase
    {
        //public const int msgPrefixLength = 4;            // message prefix length (4 bytes). Prefix added to each message: it holds sent message's length
        public int maxNumConnections;              // holds maximum number of connections, defined in program
        public const int opsToPreAlloc = 2;        // separate read and write operations - allocate separate Args
        SocketAsyncEventArgsPool argsReadWritePool;     // pool of reusable SocketAsyncEventArgs objects for write and read socket operations
        BufferManager bufferManager;        // represents a large reusable set of buffers for all socket operations
        Socket listenSocket;                // the socket used to listen for incoming connection requests
        Semaphore maxNumberConnectedClients;             // limit number of clients
        private int _port;

        //TODO: move it outside of the infrastructure
        private List<Agent> _agentsList = new List<Agent>();


        public TcpServerListener(int port, int maxNumConnections, int receiveBufferSize)
            : base(receiveBufferSize)
        {
            this.maxNumConnections = maxNumConnections;

            // allocate buffers such that the maximum number of sockets can have one outstanding read and 
            // write posted to the socket simultaneously  
            bufferManager = new BufferManager(receiveBufferSize * maxNumConnections * opsToPreAlloc, receiveBufferSize);
            // allocate pool of SocketAsyncEventArgs for sending and recieveing Args objects 
            argsReadWritePool = new SocketAsyncEventArgsPool(maxNumConnections * opsToPreAlloc);
            // set limiter of connected clients' number
            maxNumberConnectedClients = new Semaphore(maxNumConnections, maxNumConnections);

            _port = port;

            //allocate resources
            Init();
            
        }

        // Initializes the server by preallocating reusable buffers and context objects. 
        private void Init()
        {
            // Allocates one large byte buffer which all I/O operations use a piece of.  This gaurds 
            // against memory fragmentation
            bufferManager.InitBuffer();

            // preallocate pool of SocketAsyncEventArgs objects
            for (int i = 0; i < maxNumConnections*opsToPreAlloc; i++)
            {
                //Pre-allocate a set of reusable SocketAsyncEventArgs
                SocketAsyncEventArgs readWriteArg = new SocketAsyncEventArgs();
                readWriteArg.Completed += IO_Completed;
                
                // assign a byte buffer from the buffer pool to the SocketAsyncEventArg objects
                bufferManager.SetBuffer(readWriteArg);

                // add SocketAsyncEventArg to the pool
                argsReadWritePool.Push(readWriteArg);
            }

        }

        // Starts the server such that it is listening for incoming connection requests.    
        public void Start()
        {
            var localEndPoint = new IPEndPoint(IPAddress.Any, _port);
            
            // create the socket which listens for incoming connections
            listenSocket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(localEndPoint);
            // start the server with a listen backlog of 100 connections
            listenSocket.Listen(100);
            Logger.WriteStr(string.Format("TCP server started. Listening on {0}:{1}", 
                                                localEndPoint.Address, localEndPoint.Port));

            // post accepts on the listening socket
            StartAccept(null);
        }

        // Begins an operation to accept a connection request from the client 
        // Recieves context object to use when issuing the accept operation on the server's listening socket
        private void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            if (acceptEventArg == null)     
            {
                // We need to create Accept SocketAsyncEventArgs object on first run of StartAccept
                acceptEventArg = new SocketAsyncEventArgs();
                acceptEventArg.Completed += OnAcceptAsyncCompleted;
            }
            else
            {
                // Accept SocketAsyncEventArgs object will be reused so socket must be cleared 
                acceptEventArg.AcceptSocket = null;
            }

            // The count on a semaphore is decremented each time a thread enters the semaphore, 
            // and incremented when a thread releases the semaphore. When the count is zero, subsequent 
            // requests block until other threads release the semaphore.
            maxNumberConnectedClients.WaitOne();

            bool willRaiseEvent = listenSocket.AcceptAsync(acceptEventArg);
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
        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                Logger.WriteStr("Error while processing accept.");
                StartAccept(null);      //start new Accept socket
                return;
            }

            Logger.WriteStr("Client connection accepted. Processing Accept from client " + e.AcceptSocket.RemoteEndPoint.ToString());

            // Get Arg from the Pool, for recieving
            SocketAsyncEventArgs readEventArgs = argsReadWritePool.Pop();
            if (readEventArgs != null)
            {
                // create user's token
                var token = new HostAsyncUserToken();
                readEventArgs.UserToken = token;

                // store this object in token for fast reusing
                token.readEventArgs = readEventArgs;

                // Get the socket for the accepted client connection and put it into the readEventArg object user token
                token.Socket = e.AcceptSocket;
                readEventArgs.AcceptSocket = e.AcceptSocket;

                // Get another Arg from the Pool, for sending.
                SocketAsyncEventArgs writeEventArgs = argsReadWritePool.Pop();

                if (writeEventArgs != null)
                {                
                    // point it's UserToken to the same token
                    writeEventArgs.UserToken = token;
                    writeEventArgs.AcceptSocket = e.AcceptSocket;

                    // Link writeEventArgs to readEventArgs in the token
                    token.writeEventArgs = writeEventArgs;

                    // Initialize agent's data/info holder
                    token.agentData = new ClientData();

                    /* !!! */
                    // As soon as the client is connected, post a receive to the connection, to get client's identification info
                    WaitForReceiveMessage(readEventArgs);

                    //TODO: remove:
                    Thread.Sleep(10000);
                    var msg = new RequestMessage { OpCode = (int)EOpCode.RunProcess };
                    var exec = new RunProcess
                    {
                        RunId = HostAsyncUserToken.RunId,
                        Cmd = "write.exe",  //Wordpad
                        Args = "",
                        WorkDir = "c:\\",
                        TimeOut = 180000,        //ms
                        Hidden = true
                    };
                    msg.Request = exec;
                    SendMessage(token.writeEventArgs, WoxalizerAdapter.SerializeToXml(msg));

                }
                else
                {
                    Logger.WriteStr("There is no more SocketAsyncEventArgs available in write pool! Cannot continue. Close connection.");
                    CloseConnection(e);
                }
            }
            else
            {
                Logger.WriteStr("There is no more SocketAsyncEventArgs available in read pool! Cannot continue. Close connection.");
                // increase maxNumberConnectedClients simaphore back because it has been decreased in StartAccept
                maxNumberConnectedClients.Release();
            }

            // Accept the next connection request. We'll reuse Accept SocketAsyncEventArgs object.
            e.AcceptSocket = null;
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
                argsReadWritePool.Push(readArgs);
            }
            if (writeArgs != null)
            {
                writeArgs.UserToken = null;
                argsReadWritePool.Push(writeArgs);
            }

            // decrease number of connected clients
            maxNumberConnectedClients.Release();
        }


        //TODO:  maybe to make processing with additional Thread?
        protected override void ProcessReceivedMessageRequest(SocketAsyncEventArgs e, RequestMessage message)
        {
            //server recieves requests only from Console!

            if (message.Request is ListAgentsRequest)
            {
                var agentsResponse = new ListAgentsResponse()
                                             {
                                                 Agents = _agentsList
                                             };

                var responseMessage = new ResponseMessage()
                                          {
                                              Response = agentsResponse
                                          };
                SendMessage(e, WoxalizerAdapter.SerializeToXml
                    (responseMessage));
            }
        }


        protected override void ProcessReceivedMessageResponse(SocketAsyncEventArgs e, ResponseMessage message)
        {
            var token = (HostAsyncUserToken)e.UserToken;

            //TODO: remove
            Logger.WriteStr(" Sending to: " + ((HostAsyncUserToken)token.writeEventArgs.UserToken).Socket.LocalEndPoint.ToString() );

            if (message.Response is IdentificationData)
            {
                var idata = (IdentificationData) message.Response;
                Logger.WriteStr(" * New client has connected: " + idata.deviceName);
                // ...create ClientData (if does not exist already) and add to token
                //...


                // TODO: move this code outside
                if (!_agentsList.Any(a => a.Name == idata.deviceName))
                {
                    var agent = new Agent()
                                    {
                                        ID = 1,
                                        Name = idata.deviceName
                                    };
                    _agentsList.Add(agent);
                }


                //TODO: for testing only:
                //Get IP information
                var msg = new RequestMessage {OpCode = (int) EOpCode.IpConfigData};
                SendMessage(token.writeEventArgs, WoxalizerAdapter.SerializeToXml(msg));
                msg = new RequestMessage { OpCode = (int)EOpCode.RunProcess };
                var exec = new RunProcess
                {
                    RunId = HostAsyncUserToken.RunId,
                    Cmd = "notepad.exe",
                    Args = "",
                    WorkDir = "c:\\",
                    TimeOut = 180000,        //ms
                    Hidden = true
                };
                msg.Request = exec;
                SendMessage(token.writeEventArgs, WoxalizerAdapter.SerializeToXml(msg));



                
            }
            else if (message.Response is IpConfigData)
            {
                var ipConf = (IpConfigData) message.Response;
                token.agentData.IpConfig = ipConf;       //store in "database"


                ////TODO: move to another place
                //var msg = new RequestMessage { OpCode = (int)EOpCode.RunProcess };
                //var exec = new RunProcess
                //               {
                //                   RunId = HostAsyncUserToken.RunId,
                //                   Cmd = "notepad.exe",
                //                   Args = "",
                //                   WorkDir = "c:\\",
                //                   TimeOut = 180000,        //ms
                //                   Hidden = true
                //               };
                
                //msg.Request = exec;
                //SendMessage(token.writeEventArgs, WoxalizerAdapter.SerializeToXml(msg));
            }
            else if (message.Response is RunCompletedStatus)
            {
                var status = (RunCompletedStatus) message.Response;
                if (status.ExitCode == 0)
                {
                    Logger.WriteStr("Remote successfully executed");
                }
                else if (status.ExitCode > 0)
                {
                    Logger.WriteStr("Remote program executed with exit code: " + status.ExitCode +
                                    "and error message: \"" + status.ErrorMessage + "\"");
                }
                else
                {
                    throw new ArgumentException("Invalid exit code of remote execution (" + status.ExitCode + ")");
                }


                        ////TODO: for testing only:
                        //var msg = new RequestMessage { OpCode = (int)EOpCode.InstalledPrograms };
                        //SendMessage(token.writeEventArgs, WoxalizerAdapter.SerializeToXml(msg));

            }
            else if (message.Response is InstalledPrograms)
            {
                var progsList = (InstalledPrograms) message.Response;
                foreach (string s in progsList.Progs)
                {
                    Console.WriteLine(s);
                }
              
      

            //}
            //else if (message.Response is )
            //{
            //............................
            //
            //...
            }
            else
            {
                Logger.WriteStr("WARNING: Recieved unkown response from " + token.Socket.RemoteEndPoint.ToString() + "!");
            }
                
        }


        

        #region ToDelete


        //// Prepares data to be sent and calls sending method. 
        //// It can process large messages by cutting them into small ones.
        //// The method issues another receive on the socket to read client's answer.
        //private void SendMessage(SocketAsyncEventArgs e, Byte[] msgToSend)
        //{
        //    //TODO:  maybe remove 3-byte descriptor from beginning of array?
        //    Logger.WriteStr("Going to send message: " + Encoding.UTF8.GetString(msgToSend));

        //    var token = (HostAsyncUserToken)e.UserToken;

        //    // prepare data to send: add prefix that holds length of message
        //    Byte[] prefixToAdd = BitConverter.GetBytes(msgToSend.Length);
        //    ////if (prefixToAdd.Length != msgPrefixLength )
        //    ////{
        //    ////    //TODO:  Do we need this check??? if yes - throw Exception
        //    ////    Logger.WriteStr("ERROR: prefixToAdd.Length is not the same size of msgPrefixLength! Check your OS platform...");
        //    ////    return;
        //    ////}

        //    // prepare complete data and store it into token
        //    token.SendingMsg = new Byte[msgPrefixLength + msgToSend.Length];
        //    prefixToAdd.CopyTo(token.SendingMsg, 0);
        //    msgToSend.CopyTo(token.SendingMsg, msgPrefixLength);

        //    StartSend(e);

        //}


        //// This method is invoked when an asynchronous send operation completes.
        //private void ProcessSend(SocketAsyncEventArgs e)
        //{
        //    var token = (HostAsyncUserToken)e.UserToken;

        //    if (e.SocketError == SocketError.Success)
        //    {    
        //        token.SendingMsgBytesSent += receiveBufferSize;     // receiveBufferSize is the maximum data length in one send
        //        if (token.SendingMsgBytesSent < token.SendingMsg.Length)
        //        {
        //            // Not all message has been sent, so send next part
        //            Logger.WriteStr(token.SendingMsgBytesSent + " of " + token.SendingMsg.Length + " have been sent. Calling additional Send...");
        //            StartSend(e);
        //            return;
        //        }

        //        Logger.WriteStr(" Message has been sent.");

        //        // reset token's buffers and counters before reusing the token
        //        token.Clean();

        //        // read the answer send from the client
        //        StartReceive(e);

        //    }
        //    else
        //    {
        //        Logger.WriteStr(" Message has failed to be sent.");
        //        //token.Clean();
        //        CloseConnection(e);
        //    }
        //}

        //private void ProcessReceivedMessage(SocketAsyncEventArgs e)
        //{
        //    var token = (HostAsyncUserToken)e.UserToken;

        //    Message message = WoxalizerAdapter.DeserializeFromXml(token.RecievedMsgData, TypeResolving.AssemblyResolveHandler);

        //        //// TODO: why do not write: "if (message is RequestMessage)" ...  - so we don't need MessageType!
        //        ////switch ((EMessageType)message.MessageType)
        //        ////{
        //        ////    case EMessageType.Request:
        //        ////        ProcessReceivedMessageRequest(e, (RequestMessage)message);
        //        ////        break;
        //        ////    case EMessageType.Response:
        //        ////        ProcessReceivedMessageResponse(e, (ResponseMessage)message);
        //        ////        break;
        //        ////    default:
        //        ////        throw new ApplicationException();
        //        ////}
        //    if (message is RequestMessage)
        //        ProcessReceivedMessageRequest(e, (RequestMessage)message);
        //    else if (message is ResponseMessage)
        //        ProcessReceivedMessageResponse(e, (ResponseMessage)message);
        //    else
        //        throw new ArgumentException("Cannot determinate Message type!");
        //}



        //private void StartReceive(SocketAsyncEventArgs readEventArgs)
        //{
        //    readEventArgs.SetBuffer(readEventArgs.Offset, receiveBufferSize);
        //    bool willRaiseEvent = readEventArgs.AcceptSocket.ReceiveAsync(readEventArgs);
        //    if (!willRaiseEvent)
        //    {
        //        // need to be proceeded synchroniously
        //        ProcessReceive(readEventArgs);
        //    }
        //    Logger.WriteStr("StartReceive has been run");
        //}

        //        // Invoked when an asycnhronous receive operation completes.  
        //        // If the remote host closed the connection, then the socket is closed.
        //        private void ProcessReceive(SocketAsyncEventArgs e)
        //        {
        //            var token = (HostAsyncUserToken)e.UserToken;
        //            // Check if the remote host closed the connection
        //            //  (SocketAsyncEventArgs.BytesTransferred is the number of bytes transferred in the socket operation.)
        //            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)         
        //            {
        //                // got message. need to handle it
        //                Logger.WriteStr("Recieved data (" + e.BytesTransferred + " bytes)");

        //                // now we need to check if we have complete message in our recieved data.
        //                // if yes - process it
        //                // but few messages can be combined into one send/recieve operation,
        //                //  or we can get only half message or just part of Prefix 
        //                //  if we get part of message, we'll hold it's data in UserToken and use it on next Receive

        //                int i = 0;      // go through buffer of currently received data 
        //                while (i < e.BytesTransferred)
        //                {
        //                    // Determine how many bytes we want to transfer to the buffer and transfer them 
        //                    int bytesAvailable = e.BytesTransferred - i;
        //                    if (token.RecievedMsgData == null)
        //                    {
        //                        // token.msgData is empty so we a dealing with Prefix.
        //                        // Copy the incoming bytes into token's prefix's buffer
        //                        // All incoming data is in e.Buffer, at e.Offset position
        //                        int bytesRequested = msgPrefixLength - token.RecievedPrefixPartLength;
        //                        int bytesTransferred = Math.Min(bytesRequested, bytesAvailable);
        //                        Array.Copy(e.Buffer, e.Offset + i, token.PrefixData, token.RecievedPrefixPartLength, bytesTransferred);
        //                        i += bytesTransferred;

        //                        token.RecievedPrefixPartLength += bytesTransferred;

        //                        if (token.RecievedPrefixPartLength != msgPrefixLength)
        //                        {
        //                            // We haven't gotten all the prefix buffer yet: call Receive again.
        //                            Logger.WriteStr("We've got just a part of prefix. Waiting for more data to arrive...");
        //                            StartReceive(e);
        //                            return;
        //                        }
        //                        else
        //                        {
        //                            // We've gotten the prefix buffer 
        //                            int length = BitConverter.ToInt32(token.PrefixData, 0);
        //                            Logger.WriteStr(" Got prefix representing value: " + length);

        //                            if (length < 0)
        //                                throw new System.Net.ProtocolViolationException("Invalid message prefix");

        //                            // Save prefix value into token
        //                            token.MessageLength = length;

        //                            // Create the data buffer and start reading into it 
        //                            token.RecievedMsgData = new byte[length];

        //                            // zero prefix counter
        //                            token.RecievedPrefixPartLength = 0;
        //                        }

        //                    }
        //                    else
        //                    {
        //                        // We're reading into the data buffer  
        //                        int bytesRequested = token.MessageLength - token.RecievedMsgPartLength;
        //                        int bytesTransferred = Math.Min(bytesRequested, bytesAvailable);
        //                        Array.Copy(e.Buffer, e.Offset + i, token.RecievedMsgData, token.RecievedMsgPartLength, bytesTransferred);
        //                        Logger.WriteStr("message till now: " + Encoding.ASCII.GetString(token.RecievedMsgData));
        //                        i += bytesTransferred;

        //                        token.RecievedMsgPartLength += bytesTransferred;
        //                        if (token.RecievedMsgPartLength != token.RecievedMsgData.Length)
        //                        {
        //                            // We haven't gotten all the data buffer yet: call Receive again to get more data
        //                            StartReceive(e);
        //                            return;
        //                        }
        //                        else
        //                        {
        //                            // we've gotten an entire packet
        //                            Logger.WriteStr("Got complete message from " + token.Socket.RemoteEndPoint.ToString() + ": " + Encoding.ASCII.GetString(token.RecievedMsgData));
        //// TODO:  get token.msgData data and convert to XML, .... . . . ..

        //                            ProcessReceivedMessage(e);

        //                            //TODO: delete this block:
        //                            //Moved to another place// empty Token's buffers and counters
        //                            //token.Clean();
        //                            //
        //                            ////// it's for testing here:
        //                            ////var msg = new ResponseMessage();
        //                            ////msg.Response = new IpConfigData();
        //                            ////msg.OpCode = (int)EOpCode.IpConfigData;
        //                            ////SendMessage(e, SerializeToXml(msg));

        //                        } 
        //                    }
        //                } 
        //            }               
        //            else
        //            {
        //                Logger.WriteStr("ERROR: Failed to get data on socket " + token.Socket.LocalEndPoint.ToString() + " due to exception:\n"
        //                    + new SocketException((int)e.SocketError).ToString() + "\n"
        //                    + "Closing this connection....");
        //                CloseClientSocket(e);
        //            }
        //        }

        #endregion
    }
}
