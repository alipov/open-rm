using System;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace OpenRm.Common.Entities.Network
{
    public abstract class TcpBase
    {
        protected const int msgPrefixLength = 4;            // message prefix length (4 bytes). Prefix added to each message: it holds sent message's length
        protected int receiveBufferSize;
        private Func<object, ResolveEventArgs, Assembly> _assemblyResolveHandler;

        protected TcpBase(Func<object, ResolveEventArgs, Assembly> assemblyResolveHandler, int bufferSize)
        {
            _assemblyResolveHandler = assemblyResolveHandler;
            this.receiveBufferSize = bufferSize;
        }

        // Invoked when an asycnhronous receive operation completes.  
        // If the remote host closed the connection, then the socket is closed.
        private void ProcessReceive(SocketAsyncEventArgs e)
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
                    if (token.MsgData == null)
                    {
                        // token.msgData is empty so we a dealing with Prefix.
                        // Copy the incoming bytes into token's prefix's buffer
                        // All incoming data is in e.Buffer, at e.Offset position
                        int bytesRequested = msgPrefixLength - token.RecievedPrefixPartLength;
                        int bytesTransferred = Math.Min(bytesRequested, bytesAvailable);
                        Array.Copy(e.Buffer, e.Offset + i, token.PrefixData, token.RecievedPrefixPartLength, bytesTransferred);
                        i += bytesTransferred;

                        token.RecievedPrefixPartLength += bytesTransferred;

                        if (token.RecievedPrefixPartLength != msgPrefixLength)
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

                            if (length < 0)
                                throw new System.Net.ProtocolViolationException("Invalid message prefix");

                            // Save prefix value into token
                            token.MessageLength = length;

                            // Create the data buffer and start reading into it 
                            token.MsgData = new byte[length];

                            // zero prefix counter
                            token.RecievedPrefixPartLength = 0;
                        }

                    }
                    else
                    {
                        // We're reading into the data buffer  
                        int bytesRequested = token.MessageLength - token.RecievedMsgPartLength;
                        int bytesTransferred = Math.Min(bytesRequested, bytesAvailable);
                        Array.Copy(e.Buffer, e.Offset + i, token.MsgData, token.RecievedMsgPartLength, bytesTransferred);
                        Logger.WriteStr("message till now: " + Encoding.ASCII.GetString(token.MsgData));
                        i += bytesTransferred;

                        token.RecievedMsgPartLength += bytesTransferred;
                        if (token.RecievedMsgPartLength != token.MsgData.Length)
                        {
                            // We haven't gotten all the data buffer yet: call Receive again to get more data
                            StartReceive(e);
                            return;
                        }
                        else
                        {
                            // we've gotten an entire packet
                            Logger.WriteStr("Got complete message from " + token.Socket.RemoteEndPoint.ToString() + ": " + Encoding.ASCII.GetString(token.MsgData));
                            // TODO:  get token.msgData data and convert to XML, .... . . . ..

                            ProcessReceivedMessage(e);

                            //TODO: delete this block:
                            //Moved to another place// empty Token's buffers and counters
                            //token.Clean();
                            //
                            ////// it's for testing here:
                            ////var msg = new ResponseMessage();
                            ////msg.Response = new IpConfigData();
                            ////msg.OpCode = (int)EOpCode.IpConfigData;
                            ////SendMessage(e, SerializeToXml(msg));

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

        private void ProcessReceivedMessage(SocketAsyncEventArgs e)
        {
            var token = (AsyncUserTokenBase)e.UserToken;

            Message message = WoxalizerAdapter.DeserializeFromXml(token.MsgData, _assemblyResolveHandler);

            if (message is RequestMessage)
                ProcessReceivedMessageRequest(e, (RequestMessage)message);
            else if (message is ResponseMessage)
                ProcessReceivedMessageResponse(e, (ResponseMessage)message);
            else
                throw new ArgumentException("Cannot determinate Message type!");
        }

        protected abstract void ProcessReceivedMessageRequest(SocketAsyncEventArgs e, RequestMessage message);
        protected abstract void ProcessReceivedMessageResponse(SocketAsyncEventArgs e, ResponseMessage message);
        protected abstract void CloseConnection(SocketAsyncEventArgs e);
    }
}
