using System;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using OpenRm.Common.Entities.Network.Messages;

namespace OpenRm.Common.Entities.Network
{
    public abstract class TcpBase
    {
        protected const int MsgPrefixLength = 4;            // message prefix length (4 bytes). Prefix added to each message: it holds sent message's length
        protected int ReceiveBufferSize;
        private readonly Func<object, ResolveEventArgs, Assembly> _assemblyResolveHandler;

        protected TcpBase(Func<object, ResolveEventArgs, Assembly> assemblyResolveHandler, int bufferSize)
        {
            _assemblyResolveHandler = assemblyResolveHandler;
            ReceiveBufferSize = bufferSize;
        }


        protected virtual void StartReceive(SocketAsyncEventArgs readEventArgs)
        {
            Logger.WriteStr("StartReceive has been run");

            var token = (AsyncUserTokenBase)readEventArgs.UserToken;

            readEventArgs.SetBuffer(readEventArgs.Offset, ReceiveBufferSize);
            bool willRaiseEvent = readEventArgs.AcceptSocket.ReceiveAsync(readEventArgs);
            if (!willRaiseEvent)
            {
                // need to be proceeded synchroniously
                ProcessReceive(readEventArgs);
            }
        }

        // Invoked when an asycnhronous receive operation completes.  
        // If the remote host closed the connection, then the socket is closed.
        protected virtual void ProcessReceive(SocketAsyncEventArgs e)
        {
            var token = (AsyncUserTokenBase)e.UserToken;

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
                    if (token.RecievedMsgData == null)
                    {
                        // token.msgData is empty so we a dealing with Prefix.
                        // Copy the incoming bytes into token's prefix's buffer
                        // All incoming data is in e.Buffer, at e.Offset position
                        int bytesRequested = MsgPrefixLength - token.RecievedPrefixPartLength;
                        int bytesTransferred = Math.Min(bytesRequested, bytesAvailable);
                        Array.Copy(e.Buffer, e.Offset + i, token.PrefixData, token.RecievedPrefixPartLength, bytesTransferred);
                        i += bytesTransferred;

                        token.RecievedPrefixPartLength += bytesTransferred;

                        if (token.RecievedPrefixPartLength != MsgPrefixLength)
                        {
                            // We haven't gotten all the prefix buffer yet: call Receive again.
                            Logger.WriteStr("We've got just a part of prefix. Waiting for more data to arrive...");
                            StartReceive(e);
                            return;
                        }
                        else
                        {
                            // We've gotten the prefix buffer 
                            int length = BitConverter.ToInt32(token.PrefixData, 0);
                            Logger.WriteStr(" Got prefix representing value: " + length);

                            if (length == 0)
                            {
                                Logger.WriteStr("We've got keep-alive / empty message");
                                //TODO: remove Sleep:
                                Thread.Sleep(10000);
                                break;  //exit while
                            }

                            if (length < 0)
                                throw new System.Net.ProtocolViolationException("Invalid message prefix");

                            // Save prefix value into token
                            token.MessageLength = length;

                            // Create the data buffer and start reading into it 
                            token.RecievedMsgData = new byte[length];

                            // zero prefix counter
                            token.RecievedPrefixPartLength = 0;
                        }

                    }
                    else
                    {
                        // We're reading into the data buffer  
                        int bytesRequested = token.MessageLength - token.RecievedMsgPartLength;
                        int bytesTransferred = Math.Min(bytesRequested, bytesAvailable);
                        Array.Copy(e.Buffer, e.Offset + i, token.RecievedMsgData, token.RecievedMsgPartLength, bytesTransferred);
                        Logger.WriteStr("message till now: " + Encoding.ASCII.GetString(token.RecievedMsgData));
                        i += bytesTransferred;

                        token.RecievedMsgPartLength += bytesTransferred;
                        if (token.RecievedMsgPartLength != token.RecievedMsgData.Length)
                        {
                            // We haven't gotten all the data buffer yet: call Receive again to get more data
                            StartReceive(e);
                            return;
                        }
                        else
                        {
                            // we've gotten an entire packet
                            Logger.WriteStr("Got complete message from " + token.Socket.RemoteEndPoint.ToString() + ": " + Encoding.ASCII.GetString(token.RecievedMsgData));

                            ProcessReceivedMessage(e);    

                            // Clean token's recieve buffer
                            token.CleanForRecieve();

                            // wait for next message
/////////////////////                            StartReceive(e);
                        }
                    }
                }
            }
            else
            {
                Logger.WriteStr("ERROR: Failed to get data on socket " + token.Socket.LocalEndPoint.ToString() + " due to exception:\n"
                    + new SocketException((int)e.SocketError).ToString() + "\n"
                    + "Closing this connection....");
                CloseConnection(e);
            }
        }


        protected void ProcessReceivedMessage(SocketAsyncEventArgs e)
        {
            var token = (AsyncUserTokenBase)e.UserToken;

            Message message = WoxalizerAdapter.DeserializeFromXml(token.RecievedMsgData, _assemblyResolveHandler);

            if (message is RequestMessage)
                ProcessReceivedMessageRequest(e, (RequestMessage)message);
            else if (message is ResponseMessage)
                ProcessReceivedMessageResponse(e, (ResponseMessage)message);
            else
                throw new ArgumentException("Cannot determinate Message type!");
        }


        protected virtual void StartSend(SocketAsyncEventArgs e)
        {
            var token = (AsyncUserTokenBase)e.UserToken;

            // don't let sending simultatiously with ONE SocketAsyncEventArgs object
            token.writeSemaphore.WaitOne();

            int bytesToTransfer = Math.Min(ReceiveBufferSize, token.SendingMsg.Length - token.SendingMsgBytesSent);
            Array.Copy(token.SendingMsg, token.SendingMsgBytesSent, e.Buffer, e.Offset, bytesToTransfer);
            e.SetBuffer(e.Offset, bytesToTransfer);

            bool willRaiseEvent = e.AcceptSocket.SendAsync(e);
            if (!willRaiseEvent)
            {
                ProcessSend(e);
            }

            // release lock
            token.writeSemaphore.Release();
        }


        // This method is invoked when an asynchronous send operation completes.
        protected virtual void ProcessSend(SocketAsyncEventArgs e)
        {
            var token = (AsyncUserTokenBase)e.UserToken;

            if (e.SocketError == SocketError.Success)
            {
                token.SendingMsgBytesSent += ReceiveBufferSize;     // receiveBufferSize is the maximum data length in one send
                if (token.SendingMsgBytesSent < token.SendingMsg.Length)
                {
                    // Not all message has been sent, so send next part
                    Logger.WriteStr(token.SendingMsgBytesSent + " of " + token.SendingMsg.Length + " have been sent. Calling additional Send...");
                    StartSend(e);
                    return;
                }

                Logger.WriteStr(" Complete message has been sent.");

                // reset token's buffers and counters before reusing the token
                token.CleanForSend();

                // let process another send operation
                token.writeSemaphore.Release();

                ////// read the answer send from the client
                ////StartReceive(e);

            }
            else
            {
                Logger.WriteStr(" Message has failed to be sent.");
                CloseConnection(e);
            }
        }

        // Prepares data to be sent and calls sending method. 
        // It can process large messages by cutting them into small ones.
        // The method issues another receive on the socket to read client's answer.
        protected void SendMessage(SocketAsyncEventArgs e, Byte[] msgToSend)
        {
            var token = (AsyncUserTokenBase)e.UserToken;

            // do not let sending simultaniously using the same Args object 
            token.writeSemaphore.WaitOne();

            //TODO:  maybe remove 3-byte descriptor from beginning of array?
            Logger.WriteStr("Going to send message: " + Encoding.UTF8.GetString(msgToSend));

            // reset token's buffers and counters before reusing the token
            //token.Clean();

            // prepare data to send: add prefix that holds length of message
            Byte[] prefixToAdd = BitConverter.GetBytes(msgToSend.Length);

            // prepare complete data and store it into token
            token.SendingMsg = new Byte[MsgPrefixLength + msgToSend.Length];
            prefixToAdd.CopyTo(token.SendingMsg, 0);
            msgToSend.CopyTo(token.SendingMsg, MsgPrefixLength);

            StartSend(e);
        }

        protected abstract void ProcessReceivedMessageRequest(SocketAsyncEventArgs e, RequestMessage message);
        protected abstract void ProcessReceivedMessageResponse(SocketAsyncEventArgs e, ResponseMessage message);
        protected abstract void CloseConnection(SocketAsyncEventArgs e);

        public abstract void Start();
    }
}
