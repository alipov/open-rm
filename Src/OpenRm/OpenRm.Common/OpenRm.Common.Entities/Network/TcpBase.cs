using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace OpenRm.Common.Entities.Network
{
    public abstract class TcpBase
    {
        protected const int MsgPrefixLength = 4;            // message prefix length (4 bytes). Prefix added to each message: it holds sent message's length
        protected int ReceiveBufferSize;

        protected static Encoding utf8 = new UTF8Encoding(false);    //False means there is NO "Byte Order Mark" (three bytes appended at beginning of byte array)

        protected TcpBase(int bufferSize)
        {
            //_assemblyResolveHandler = assemblyResolveHandler;
            ReceiveBufferSize = bufferSize;
        }


        protected virtual void WaitForReceiveMessage(SocketAsyncEventArgs readEventArgs)
        {
            //TODO: remove these lines
            var token = (AsyncUserTokenBase)readEventArgs.UserToken;
            token.readSemaphore.WaitOne();

            Logger.WriteStr(" Waiting for new data to arrive...");
            StartReceive(readEventArgs);
        }


        protected virtual void StartReceive(SocketAsyncEventArgs readEventArgs)
        {
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
        protected virtual void ProcessReceive(SocketAsyncEventArgs args)
        {
            var token = (AsyncUserTokenBase)args.UserToken;

            // Check if the remote host closed the connection
            //  (SocketAsyncEventArgs.BytesTransferred is the number of bytes transferred/received in the socket operation.)
            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
            {
                // got message. need to handle it
                Logger.WriteStr("   Recieved data (" + args.BytesTransferred + " bytes)");

                // now we need to check if we have complete message in our recieved data.
                // if yes - process it
                // but few messages can be combined into one send/recieve operation,
                //  or we can get only half message or just part of Prefix 
                //  if we get part of message, we'll hold it's data in UserToken and use it on next Receive

                int i = 0;      // go through buffer of currently received data 
                while (i < args.BytesTransferred)
                {
                    // Determine how many bytes we want to transfer to the buffer and transfer them 
                    int bytesAvailable = args.BytesTransferred - i;
                    //if (token.RecievedMsgData == null && )
                    if (token.RecievedPrefixPartLength < MsgPrefixLength)
                    {
                        // We are dealing with Prefix.
                        // Copy the incoming bytes into token's prefix's buffer
                        // All incoming data is in e.Buffer, at e.Offset position
                        int bytesRequested = MsgPrefixLength - token.RecievedPrefixPartLength;
                        int bytesTransferred = Math.Min(bytesRequested, bytesAvailable);
                        Array.Copy(args.Buffer, args.Offset + i, token.PrefixData, token.RecievedPrefixPartLength, bytesTransferred);
                        i += bytesTransferred;

                        token.RecievedPrefixPartLength += bytesTransferred;

                        if (token.RecievedPrefixPartLength != MsgPrefixLength)
                        {
                            // We haven't gotten all the prefix buffer yet: call Receive again.
                            Logger.WriteStr(" We've got just a part of prefix. Waiting for more data to arrive...");
                            StartReceive(args);
                            return;
                        }

                        // We've gotten the prefix buffer 
                        int length = BitConverter.ToInt32(token.PrefixData, 0);
                        Logger.WriteStr(" Got prefix representing value: " + length);

                        if (length == 0)
                        {
                            Logger.WriteStr(" We've got keep-alive message");
                            // we don't process such messages
                            break;  //exit while
                        }

                        if (length < 0)
                            throw new ProtocolViolationException(" Invalid message prefix");

                        // Save prefix value into token
                        token.MessageLength = length;

                        // Create the data buffer and start reading into it 
                        token.RecievedMsgData = new byte[length];

                        // We got full prefix, but no have data yet.
                        // If there is no more available data in out token's buffer, so we have to request more data from OS
                        // (our "while" loop will not process this case, so we should call Recieve explicitly)
                        if (i == args.BytesTransferred)         
                        {
                            Logger.WriteStr("   We've got a full prefix. Waiting for data to arrive...");
                            StartReceive(args);
                            return;
                        }

                    }
                    else
                    {
                        // We're reading into the data buffer  
                        int bytesRequested = token.MessageLength - token.RecievedMsgPartLength;
                        int bytesTransferred = Math.Min(bytesRequested, bytesAvailable);
                        Array.Copy(args.Buffer, args.Offset + i, token.RecievedMsgData, token.RecievedMsgPartLength, bytesTransferred);
                        //Logger.WriteStr("message till now: " + utf8.GetString(token.RecievedMsgData));
                        i += bytesTransferred;

                        token.RecievedMsgPartLength += bytesTransferred;
                        if (token.RecievedMsgPartLength < token.RecievedMsgData.Length)
                        {
                            // We haven't gotten all the data buffer yet: call Receive again to get more data
                            StartReceive(args);
                            return;
                        }

                        // we've gotten an entire packet
                        Logger.WriteStr("   Got complete message from " + token.Socket.RemoteEndPoint + " with length=" + token.RecievedMsgData.Length + ".");

                        ProcessReceivedMessage(args);

                        // Check if something left in the buffer: maybe there is a start of next message
                        int bytesLeft = bytesAvailable - bytesTransferred;
                        if (bytesLeft > 0)
                        {
                            Logger.WriteStr( "   " + bytesLeft + " bytes left in the byffer. It should be a part of next message. (DEBUG: i=" + i + ")");

                            // Clean token's recieve buffer for next message that should be waiting in the buffer
                            token.CleanForRecieve();

                            // continue in "while" loop
                        }
                        else
                        {
                            //TODO: remove?
                            break;  //break while
                        }

                        
                    }
                }   //end while

                // Clean token's recieve buffer
                token.CleanForRecieve();

                //// we've proceeded a whole message so release the lock
                token.readSemaphore.Release();

                // start waiting for next message
                WaitForReceiveMessage(args);
            }
            else
            {
                Logger.WriteStr(" ERROR: Failed to get data on socket " + token.Socket.LocalEndPoint + 
                    " due to exception:\n" + new SocketException((int)args.SocketError));

                ProcessReceiveFailure(args);
            }
        }

        protected abstract void ProcessReceiveFailure(SocketAsyncEventArgs e);
        

        // Prepares data to be sent and calls sending method. 
        // It can process large messages by cutting them into small ones.
        public void SendMessage(AsyncUserTokenBase token, Byte[] msgToSend)
        {
            // do not let sending simultaniously using the same Args object 
            token.writeSemaphore.WaitOne();

            // pause keep-alive messages (will resume after sending)
            token.KeepAliveTimer.Stop();

            if (msgToSend.Length == 0)
                Logger.WriteStr(" Going to send keep-alive message.");
            else
                Logger.WriteStr(" Going to send message with length=" + msgToSend.Length + ".");

            // prepare data to send: add prefix that holds length of message
            Byte[] prefixToAdd = BitConverter.GetBytes(msgToSend.Length);

            // prepare complete data and store it into token
            token.SendingMsg = new Byte[MsgPrefixLength + msgToSend.Length];
            prefixToAdd.CopyTo(token.SendingMsg, 0);
            msgToSend.CopyTo(token.SendingMsg, MsgPrefixLength);

            StartSend(token.writeEventArgs);
        }


        protected virtual void StartSend(SocketAsyncEventArgs e)
        {
            var token = (AsyncUserTokenBase)e.UserToken;

            int bytesToTransfer = Math.Min(ReceiveBufferSize, token.SendingMsg.Length - token.SendingMsgBytesSent);
            Array.Copy(token.SendingMsg, token.SendingMsgBytesSent, e.Buffer, e.Offset, bytesToTransfer);
            e.SetBuffer(e.Offset, bytesToTransfer);

            bool willRaiseEvent = e.AcceptSocket.SendAsync(e);      // calling asynchronous send
            if (!willRaiseEvent)
            {
                ProcessSend(e);
            }
        }


        // This method is invoked when an asynchronous send operation completes.
        protected virtual void ProcessSend(SocketAsyncEventArgs args)
        {
            var token = (AsyncUserTokenBase)args.UserToken;

            if (args.SocketError == SocketError.Success)
            {
                token.SendingMsgBytesSent += ReceiveBufferSize;     // receiveBufferSize is the maximum data length in one send
                if (token.SendingMsgBytesSent < token.SendingMsg.Length)
                {
                    // Not all message has been sent, so send next part
                    Logger.WriteStr("   " + token.SendingMsgBytesSent + " of " + token.SendingMsg.Length + " have been sent. Calling additional Send...");
                    StartSend(args);
                    return;
                }

                Logger.WriteStr(" Complete message has been sent.");

                // reset token's buffers and counters before reusing the token
                token.CleanForSend();

                // let process another send operation
                token.writeSemaphore.Release();

                // return keep-alive messages 
                token.KeepAliveTimer.Start();
            }
            else
            {
                Logger.WriteStr(" ERROR: Message has failed to be sent.");
                ProcessSendFailure(args);
            }
        }


        public void SendKeepAlive(object sender, EventArgs e)
        {
            AsyncUserTokenBase token = ((KeepAliveTimer) sender).Token;
            SendMessage(token, new byte[0]);
        }


        protected abstract void ProcessSendFailure(SocketAsyncEventArgs args);
        protected abstract void ProcessReceivedMessage(SocketAsyncEventArgs e);
        protected abstract void CloseConnection(SocketAsyncEventArgs e);

        protected abstract void ProcessFailure(SocketAsyncEventArgs e);
    }
}
