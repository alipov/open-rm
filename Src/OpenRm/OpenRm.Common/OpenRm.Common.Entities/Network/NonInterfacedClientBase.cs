using System;
using System.Net.Sockets;
using System.Text;
using OpenRm.Common.Entities.Network.Messages;

namespace OpenRm.Common.Entities.Network
{
    public abstract class NonInterfacedClientBase : TcpBase
    {
        protected NonInterfacedClientBase(int bufferSize) : base(bufferSize)
        {
        }

        protected override void ProcessReceivedMessage(SocketAsyncEventArgs e)
        {
            var token = (AsyncUserTokenBase)e.UserToken;

            // decrypt message data
            var decryptedMsgData = EncryptionAdapter.Decrypt(token.RecievedMsgData);
            Logger.WriteStr(" Recieved message was decrypted as xml: " + utf8.GetString(decryptedMsgData));

            // deserialize to object
            Message message = null;
            try
            {
                message = WoxalizerAdapter.DeserializeFromXml(decryptedMsgData);
            }
            catch(Exception ex)
            {
                Logger.WriteStr("ERROR: Cannot deserialize recieved message (" + ex.Message + "). Aborting connection.");
                CloseConnection(e);
            }

            if (message is RequestMessage)
                ProcessReceivedMessageRequest(e, (RequestMessage)message);
            else if (message is ResponseMessage)
                ProcessReceivedMessageResponse(e, (ResponseMessage)message);
            else
                throw new ArgumentException("Cannot determinate Message type!");
        }

        protected abstract void ProcessReceivedMessageRequest(SocketAsyncEventArgs e, RequestMessage message);
        protected abstract void ProcessReceivedMessageResponse(SocketAsyncEventArgs e, ResponseMessage message);
    }
}
