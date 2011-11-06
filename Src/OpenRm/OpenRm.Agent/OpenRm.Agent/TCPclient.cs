using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using OpenRm.Common.Entities;
using Woxalizer;
using System.Reflection;
using System.IO;
using System.Xml;


namespace OpenRm.Agent
{    

    class TCPclient
    {
        public const int msgPrefixLength = 4;            // message prefix length (4 bytes). Prefix added to each message: it holds sent message's length
        private byte[] sendReceiveBuffer;        // buffer for sending/receiving data by TCP layer. 
                                                        // Note that it has fixed size (same as for server, but also can be different)
            
        private int retryIntervalCurrent;        // Interval between reconnects to server (when was unable to connect / has been disconnected)
        private int retryIntervalInitial = 5;    // 5 sec.
        private int retryIntervalMaximum = 60;   // 60 sec.

        private string _serverIP = "";
        private int _serverPort = 0;

        public static ManualResetEvent clientDone = new ManualResetEvent(false);

        public TCPclient(string serverIP, int serverPort, int bufferSize)
        {
            // Initialize buffer for sending/receiving data by TCP layer. 
            sendReceiveBuffer = new byte[bufferSize];
            _serverIP = serverIP;
            _serverPort = serverPort;

            EstablishConnection();

            // pause current thread
            clientDone.WaitOne();

            Console.WriteLine("Client has been terminated.");
        }


        // Create a socket and connect to the server
        private void EstablishConnection()
        {
            Socket socket = new Socket((IPAddress.Parse(_serverIP)).AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            SocketAsyncEventArgs sockArgs = new SocketAsyncEventArgs();
            sockArgs.Completed += new EventHandler<SocketAsyncEventArgs>(SocketEventArg_Completed);
            sockArgs.RemoteEndPoint = new IPEndPoint(IPAddress.Parse(_serverIP), _serverPort);
            sockArgs.UserToken = new AsyncUserToken();
            ((AsyncUserToken)sockArgs.UserToken).socket = socket;
            sockArgs.AcceptSocket = socket;

            socket.ConnectAsync(sockArgs);
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
                    CloseServerConnection(e);
                    throw new Exception("Invalid operation completed");
            }
        }


        // Called when a ConnectAsync operation completes
        private void ProcessConnect(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                Logger.WriteStr("Successfully connected to the server on " + ((AsyncUserToken)(e.UserToken)).socket.LocalEndPoint.ToString());
                retryIntervalCurrent = retryIntervalInitial;          // set it to initial value

                //Send authorization info about this client as soon as connection established
                var idata = new IdentificationData();
                DataRetriever.GetInfo(idata);       // fill required data
                var message = new ResponseMessage {Response = idata};
                SendMessage(e, SerializeToXml(message));
                
            }
            else
            {
                int ex = (int)e.SocketError;
                Console.WriteLine("Cannot connect to server " + e.RemoteEndPoint.ToString() + ". Will try again in " + retryIntervalCurrent + " seconds");
                Logger.WriteStr("Cannot connect to server " + e.RemoteEndPoint.ToString() + ". Will try again in " + retryIntervalCurrent + " seconds");
                Logger.WriteStr("   (Exception: " + ex.ToString() + ")");

                Thread.Sleep(retryIntervalCurrent * 1000);
                if (retryIntervalCurrent < retryIntervalMaximum)     //increase interval between reconnects up to retryIntervalMaximum value.
                    retryIntervalCurrent += 5;         

                e.SocketError = 0;  //clear Error info and try to connect again
                ((AsyncUserToken)e.UserToken).socket.ConnectAsync(e);

                return;
            }
        }


        // Prepares data to be sent and calls sending method. 
        // It can process large messages by cutting them into small ones.
        // The method issues another receive on the socket to read client's answer.
        private void SendMessage(SocketAsyncEventArgs e, Byte[] msgToSend)
        {
            Logger.WriteStr("Going to send message: " + Encoding.UTF8.GetString(msgToSend));

            AsyncUserToken token = (AsyncUserToken)e.UserToken;

            // prepare data to send: add prefix that holds length of message
            Byte[] prefixToAdd = BitConverter.GetBytes(msgToSend.Length);
            if (prefixToAdd.Length != msgPrefixLength)
            {
                //TODO:  Do we need this check??? if yes - throw Exception
                Logger.WriteStr("ERROR: prefixToAdd.Length is not the same size of msgPrefixLength! Check your OS platform...");
                return;
            }

            // prepare complete data and store it into token
            token.sendingMsg = new Byte[msgPrefixLength + msgToSend.Length];
            prefixToAdd.CopyTo(token.sendingMsg, 0);
            msgToSend.CopyTo(token.sendingMsg, msgPrefixLength);


            StartSend(e);

        }


        private void StartSend(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = (AsyncUserToken)e.UserToken;

            int bytesToTransfer = Math.Min(sendReceiveBuffer.Length, token.sendingMsg.Length - token.sendingMsgBytesSent);
            e.SetBuffer(sendReceiveBuffer, 0, bytesToTransfer);
            Array.Copy(token.sendingMsg, token.sendingMsgBytesSent, e.Buffer, e.Offset, bytesToTransfer);

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
                token.sendingMsgBytesSent += sendReceiveBuffer.Length;     // receiveBufferSize is the maximum data length in one send
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

                // start to wait for new command from server
                StartReceive(e);

            }
            else
            {
                Logger.WriteStr(" Message has failed to be sent due to error: " + e.SocketError);
                CloseServerConnection(e);
            }
        }



        private void StartReceive(SocketAsyncEventArgs readEventArgs)
        {
            readEventArgs.SetBuffer(readEventArgs.Offset, sendReceiveBuffer.Length);
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
                            // We haven't got all the prefix buffer yet. Call StartReceive again to receive remaining data from TCP buffer
                            Logger.WriteStr("We've got just a part of prefix. Trying to get more data...");
                            // TODO:  maybe need to call Receive() again or just wait?
                            StartReceive(e);
                            return;
                        }
                        else
                        {
                            // We've gotten the length buffer 
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
                            // We haven't gotten all the data buffer yet: just wait for more data to arrive
                            // TODO:  maybe need to call Receive() again
                            StartReceive(e);
                            return;
                        }
                        else
                        {
                            // we've gotten an entire packet
                            Logger.WriteStr("Got complete message from " + e.RemoteEndPoint.ToString() + ": " + Encoding.ASCII.GetString(token.msgData));

                            ProcessReceivedMessage(e);

                            //// empty Token's buffers and counters
                            //token.Clean();

                            //TODO:  remove sending this data (it's for testing here)
                            //SendMessage(e, "0i9i8i7i6i5i4i3i2i1u0u9u8u7u6u5u4u3u2u1");

                        }
                    }
                }
            }
            else
            {
                Logger.WriteStr("ERROR: Failed to get data on socket " + token.socket.LocalEndPoint.ToString() + " due to exception:\n"
                    + new SocketException((int)e.SocketError).ToString() + "\n"
                    + "Closing this connection....");
                CloseServerConnection(e);
            }
        }


        private void CloseServerConnection(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = (AsyncUserToken)e.UserToken;

            // close the socket
            try
            {
                token.socket.Shutdown(SocketShutdown.Send);
            }
            // throws if server has already closed connection
            catch (Exception) { }

            Logger.WriteStr("Client disconnected from server.");

            token.Clean();
            token.socket.Close();

            if (App.agentStarted)
            {
                Logger.WriteStr("Client should be running so just try to reconnect to server...");
                retryIntervalCurrent = retryIntervalInitial;  //reset to initial value
                
                //Reconnect to server
                EstablishConnection();
            }
            else
            {
                token.socket.Close();
                clientDone.Reset();
            }
        }


        private void ProcessReceivedMessage(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = (AsyncUserToken)e.UserToken;

            Message message = DeserializeFromXml(token.msgData);

            if (message is RequestMessage)
                ProcessReceivedMessageRequest(e, (RequestMessage)message);
            else if (message is ResponseMessage)
                ProcessReceivedMessageResponse(e, (ResponseMessage)message);
            else
                throw new ArgumentException("Cannot determinate Message type!");
        }



        private void ProcessReceivedMessageRequest(SocketAsyncEventArgs e, RequestMessage message)
        {
            AsyncUserToken token = (AsyncUserToken)e.UserToken;

            switch (message.OpCode)
            {
                case (int)EOpCode.IpConfigData:
                    var ipdata = new IpConfigData();
                    DataRetriever.GetInfo(ipdata);       // fill required data
                    var responseMsg = new ResponseMessage {Response = ipdata};
                    SendMessage(e, SerializeToXml(responseMsg));

                    break;
                case (int)EOpCode.RunProcess:
                    //TODO:  Add all OpCodes...






                    break;
                default:
                    throw new ArgumentException("WARNING: Got unknown operation code request!");

            }
        }

        private void ProcessReceivedMessageResponse(SocketAsyncEventArgs e, ResponseMessage message)
        {
            //TODO: what info client needs from server?
        }


        // TODO:  move to another class?
        private static Byte[] SerializeToXml(Message msg)
        {
            var mem = new MemoryStream();
            var writer = XmlWriter.Create(mem);

            //TODO:  how to change this code to generic?
            using (var woxalizer = new WoxalizerUtil(App.AssemblyResolveHandler))
            {
                if (msg is RequestMessage)
                {
                    woxalizer.Save((RequestMessage)msg, writer);
                }
                else if (msg is ResponseMessage)
                {
                    woxalizer.Save((ResponseMessage)msg, writer);
                }
                else
                {
                    Logger.WriteStr("ERROR in serialization method: cannot determinate message type.");
                }
            }

            return mem.ToArray();
        }

        private static Message DeserializeFromXml(Byte[] msg)
        {
            Message message;
            var mem = new MemoryStream(msg);
            var reader = XmlReader.Create(mem);

            using (var woxalizer = new WoxalizerUtil(App.AssemblyResolveHandler))
            {
                message = (Message)woxalizer.Load(reader);
            }
            return message;
        }


        //static Assembly AssemblyResolveHandler(object sender, ResolveEventArgs args)
        //{
        //    //This handler is called only when the common language runtime tries to bind to the assembly and fails.

        //    //Retrieve the list of referenced assemblies in an array of AssemblyName.
        //    var assemblyPath = string.Empty;

        //    Assembly objExecutingAssemblies = Assembly.GetExecutingAssembly();
        //    AssemblyName[] arrReferencedAssmbNames = objExecutingAssemblies.GetReferencedAssemblies();

        //    //Loop through the array of referenced assembly names.
        //    foreach (AssemblyName strAssmbName in arrReferencedAssmbNames)
        //    {
        //        var requestedAssembly = args.Name.Substring(0, args.Name.IndexOf(","));

        //        //Check for the assembly names that have raised the "AssemblyResolve" event.
        //        if (strAssmbName.FullName.Substring(0, strAssmbName.FullName.IndexOf(",")) == requestedAssembly)
        //        {
        //            //Build the path of the assembly from where it has to be loaded.
        //            var rootDirecotory = Directory.GetParent(Directory.GetCurrentDirectory());
        //            assemblyPath = Directory.GetFiles
        //                            (rootDirecotory.FullName, requestedAssembly + ".dll", SearchOption.AllDirectories).Single();
        //            break;
        //            //assemblyPath = Path.Combine(rootDirecotory.FullName, "Common", requestedAssembly + ".dll");
        //        }
        //    }
        //    //Load the assembly from the specified path.
        //    Assembly myAssembly = null;

        //    // failing to ignore queries for satellite resource assemblies or using [assembly: NeutralResourcesLanguage("en-US", UltimateResourceFallbackLocation.MainAssembly)] 
        //    // in AssemblyInfo.cs will crash the program on non en-US based system cultures.
        //    if (!string.IsNullOrWhiteSpace(assemblyPath))
        //        myAssembly = Assembly.LoadFrom(assemblyPath);

        //    //Return the loaded assembly.
        //    return myAssembly;
        //}


        
        

    }
}
