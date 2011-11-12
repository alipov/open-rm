using System;
using System.Reflection;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using OpenRm.Common.Entities;
using OpenRm.Common.Entities.Network;
using OpenRm.Common.Entities.Network.Messages;

namespace OpenRm.Agent
{    
    internal class TcpClient : TcpBase
    {
        //public const int msgPrefixLength = 4;            // message prefix length (4 bytes). Prefix added to each message: it holds sent message's length
        private byte[] sendReceiveBuffer;        // buffer for sending/receiving data by TCP layer. 
                                                        // Note that it has fixed size (same as for server, but also can be different)
            
        private int retryIntervalCurrent;        // Interval between reconnects to server (when was unable to connect / has been disconnected)
        private int retryIntervalInitial = 5;    // 5 sec.
        private int retryIntervalMaximum = 60;   // 60 sec.

        private string _serverIP = "";
        private int _serverPort;

        public static ManualResetEvent clientDone = new ManualResetEvent(false);

        public TcpClient(string serverIP, int serverPort, int bufferSize, Func<object, ResolveEventArgs, Assembly> resolver)
            : base(resolver, bufferSize)
        {
            // Initialize buffer for sending/receiving data by TCP layer. 
            sendReceiveBuffer = new byte[bufferSize];
            _serverIP = serverIP;
            _serverPort = serverPort;
        }

        public override void Start()
        {
            EstablishConnection();

            // pause current thread
            clientDone.WaitOne();

            Console.WriteLine("Client has been terminated.");
        }


        // Create a socket and connect to the server
        private void EstablishConnection()
        {
            var socket = new Socket((IPAddress.Parse(_serverIP)).AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            var sockArgs = new SocketAsyncEventArgs();
            sockArgs.Completed += new EventHandler<SocketAsyncEventArgs>(SocketEventArg_Completed);
            sockArgs.RemoteEndPoint = new IPEndPoint(IPAddress.Parse(_serverIP), _serverPort);
            sockArgs.UserToken = new AgentAsyncUserToken();
            ((AgentAsyncUserToken)sockArgs.UserToken).Socket = socket;
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
                    CloseConnection(e);
                    throw new Exception("Invalid operation completed");
            }
        }


        // Called when a ConnectAsync operation completes
        private void ProcessConnect(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                Logger.WriteStr("Successfully connected to the server on " + ((AgentAsyncUserToken)(e.UserToken)).Socket.LocalEndPoint.ToString());
                retryIntervalCurrent = retryIntervalInitial;          // set it to initial value

                //Send authorization info about this client as soon as connection established
                var idata = new IdentificationData();
                OpProcessor.GetInfo(idata);       // fill required data
                var message = new ResponseMessage {Response = idata};
                SendMessage(e, WoxalizerAdapter.SerializeToXml(message, TypeResolving.AssemblyResolveHandler));
                
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
                ((AgentAsyncUserToken)e.UserToken).Socket.ConnectAsync(e);

                return;
            }
        }

        protected override void StartSend(SocketAsyncEventArgs e)
        {
            var token = (AgentAsyncUserToken)e.UserToken;

            int bytesToTransfer = Math.Min(sendReceiveBuffer.Length, token.SendingMsg.Length - token.SendingMsgBytesSent);
            e.SetBuffer(sendReceiveBuffer, 0, bytesToTransfer);
            Array.Copy(token.SendingMsg, token.SendingMsgBytesSent, e.Buffer, e.Offset, bytesToTransfer);

            bool willRaiseEvent = e.AcceptSocket.SendAsync(e);
            if (!willRaiseEvent)
            {
                ProcessSend(e);
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

            token.Clean();
            token.Socket.Close();

            if (App.agentStarted)
            {
                Logger.WriteStr("Client should be running so just try to reconnect to server...");
                retryIntervalCurrent = retryIntervalInitial;  //reset to initial value
                
                //Reconnect to server
                EstablishConnection();
            }
            else
            {
                token.Socket.Close();
                clientDone.Reset();
            }
        }

        protected override void ProcessReceivedMessageRequest(SocketAsyncEventArgs e, RequestMessage message)
        {
            var token = (AgentAsyncUserToken)e.UserToken;

            ResponseMessage responseMsg;
            switch (message.OpCode)
            {
                case (int)EOpCode.IpConfigData:
                    var ipdata = new IpConfigData();
                    OpProcessor.GetInfo(ipdata, ((IPEndPoint)token.Socket.LocalEndPoint).Address.ToString());       // fill required data
                    responseMsg = new ResponseMessage {Response = ipdata};
                    SendMessage(e, WoxalizerAdapter.SerializeToXml(responseMsg, TypeResolving.AssemblyResolveHandler));
                    break;

                case (int)EOpCode.RunProcess:
                    RunCompletedStatus result = OpProcessor.StartProcess((RunProcess)message.Request);
                    responseMsg = new ResponseMessage { Response = result };
                    SendMessage(e, WoxalizerAdapter.SerializeToXml(responseMsg, TypeResolving.AssemblyResolveHandler));
                    break;

                case (int)EOpCode.OsInfo:
                    var os = new OsInfo();
                    OpProcessor.GetInfo(os);
                    responseMsg = new ResponseMessage { Response = os };
                    SendMessage(e, WoxalizerAdapter.SerializeToXml(responseMsg, TypeResolving.AssemblyResolveHandler));
                    break;

                case (int)EOpCode.PerfmonData:
                    var pf = new PerfmonData();
                    //fetch system disk
                    //token.Data.
                    //...


                //    //TODO:  Add all OpCodes...

                    break;
                default:
                    throw new ArgumentException("WARNING: Got unknown operation code request!");

            }
        }

        protected override void ProcessReceivedMessageResponse(SocketAsyncEventArgs e, ResponseMessage message)
        {
            //TODO: what info client needs from server?
        }

        #region ToDelete

        //// This method is invoked when an asynchronous send operation completes.
        //private void ProcessSend(SocketAsyncEventArgs e)
        //{
        //    var token = (AgentAsyncUserToken)e.UserToken;

        //    if (e.SocketError == SocketError.Success)
        //    {
        //        token.SendingMsgBytesSent += sendReceiveBuffer.Length;     // receiveBufferSize is the maximum data length in one send
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

        //        // start to wait for new command from server
        //        StartReceive(e);

        //    }
        //    else
        //    {
        //        Logger.WriteStr(" Message has failed to be sent due to error: " + e.SocketError);
        //        CloseConnection(e);
        //    }
        //}

        //// Prepares data to be sent and calls sending method. 
        //// It can process large messages by cutting them into small ones.
        //// The method issues another receive on the socket to read client's answer.
        //private void SendMessage(SocketAsyncEventArgs e, Byte[] msgToSend)
        //{
        //    Logger.WriteStr("Going to send message: " + Encoding.UTF8.GetString(msgToSend));

        //    AgentAsyncUserToken token = (AgentAsyncUserToken)e.UserToken;

        //    // reset token's buffers and counters before reusing the token
        //    token.Clean();

        //    // prepare data to send: add prefix that holds length of message
        //    Byte[] prefixToAdd = BitConverter.GetBytes(msgToSend.Length);
        //    //if (prefixToAdd.Length != msgPrefixLength)
        //    //{
        //    //    //TODO:  Do we need this check??? if yes - throw Exception
        //    //    Logger.WriteStr("ERROR: prefixToAdd.Length is not the same size of msgPrefixLength! Check your OS platform...");
        //    //    return;
        //    //}

        //    // prepare complete data and store it into token
        //    token.SendingMsg = new Byte[msgPrefixLength + msgToSend.Length];
        //    prefixToAdd.CopyTo(token.SendingMsg, 0);
        //    msgToSend.CopyTo(token.SendingMsg, msgPrefixLength);

        //    StartSend(e);
        //}


        //private void ProcessReceivedMessage(SocketAsyncEventArgs e)
        //{
        //    var token = (AgentAsyncUserToken)e.UserToken;

        //    Message message = WoxalizerAdapter.DeserializeFromXml(token.MsgData, TypeResolving.AssemblyResolveHandler);

        //    if (message is RequestMessage)
        //        ProcessReceivedMessageRequest(e, (RequestMessage)message);
        //    else if (message is ResponseMessage)
        //        ProcessReceivedMessageResponse(e, (ResponseMessage)message);
        //    else
        //        throw new ArgumentException("Cannot determinate Message type!");
        //}

        //private void StartReceive(SocketAsyncEventArgs readEventArgs)
        //{
        //    readEventArgs.SetBuffer(readEventArgs.Offset, sendReceiveBuffer.Length);
        //    bool willRaiseEvent = readEventArgs.AcceptSocket.ReceiveAsync(readEventArgs);
        //    if (!willRaiseEvent)
        //    {
        //        // need to be proceeded synchroniously
        //        ProcessReceive(readEventArgs);
        //    }
        //    Logger.WriteStr("StartReceive has been run");
        //}

        //// Invoked when an asycnhronous receive operation completes.  
        //// If the remote host closed the connection, then the socket is closed.
        //private void ProcessReceive(SocketAsyncEventArgs e)
        //{
        //    var token = (AgentAsyncUserToken)e.UserToken;
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
        //            if (token.MsgData == null)
        //            {
        //                // token.msgData is empty so we a dealing with Prefix.
        //                // Copy the incoming bytes into token's prefix's buffer
        //                // All incoming data is in e.Buffer, at e.Offset position
        //                int bytesRequested = msgPrefixLength - token.RecievedPrefixPartLength;
        //                int bytesTransferred = Math.Min(bytesRequested, bytesAvailable);
        //                Array.Copy(e.Buffer, e.Offset + i, token.PrefixData, token.RecievedPrefixPartLength, bytesTransferred);
        //                i += bytesTransferred;

        //                token.RecievedPrefixPartLength += bytesTransferred;

        //                if (token.RecievedPrefixPartLength != msgPrefixLength)
        //                {
        //                    // We haven't got all the prefix buffer yet. Call StartReceive again to receive remaining data from TCP buffer
        //                    Logger.WriteStr("We've got just a part of prefix. Trying to get more data...");
        //                    StartReceive(e);
        //                    return;
        //                }
        //                else
        //                {
        //                    // We've gotten the length buffer 
        //                    int length = BitConverter.ToInt32(token.PrefixData, 0);
        //                    Logger.WriteStr(" Got prefix representing value: " + length);

        //                    if (length < 0)
        //                        throw new System.Net.ProtocolViolationException("Invalid message prefix");

        //                    // Save prefix value into token
        //                    token.MessageLength = length;

        //                    // Create the data buffer and start reading into it 
        //                    token.MsgData = new byte[length];

        //                    // zero prefix counter
        //                    token.RecievedPrefixPartLength = 0;
        //                }

        //            }
        //            else
        //            {
        //                // We're reading into the data buffer  
        //                int bytesRequested = token.MessageLength - token.RecievedMsgPartLength;
        //                int bytesTransferred = Math.Min(bytesRequested, bytesAvailable);
        //                Array.Copy(e.Buffer, e.Offset + i, token.MsgData, token.RecievedMsgPartLength, bytesTransferred);
        //                Logger.WriteStr("message till now: " + Encoding.ASCII.GetString(token.MsgData));
        //                i += bytesTransferred;

        //                token.RecievedMsgPartLength += bytesTransferred;
        //                if (token.RecievedMsgPartLength != token.MsgData.Length)
        //                {
        //                    // We haven't gotten all the data buffer yet: just wait for more data to arrive
        //                    StartReceive(e);
        //                    return;
        //                }
        //                else
        //                {
        //                    // we've gotten an entire packet
        //                    Logger.WriteStr("Got complete message from " + e.RemoteEndPoint.ToString() + ": " + Encoding.ASCII.GetString(token.MsgData));

        //                    ProcessReceivedMessage(e);

        //                    //// empty Token's buffers and counters
        //                    //token.Clean();

        //                }
        //            }
        //        }
        //    }
        //    else
        //    {
        //        Logger.WriteStr("ERROR: Failed to get data on socket " + token.Socket.LocalEndPoint.ToString() + " due to exception:\n"
        //            + new SocketException((int)e.SocketError).ToString() + "\n"
        //            + "Closing this connection....");
        //        CloseServerConnection(e);
        //    }
        //}

        #endregion
    }
}
