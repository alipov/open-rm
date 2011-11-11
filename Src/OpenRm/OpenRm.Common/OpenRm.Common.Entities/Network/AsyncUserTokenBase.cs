using System;
using System.Net.Sockets;

namespace OpenRm.Common.Entities.Network
{
    public abstract class AsyncUserTokenBase
    {
        public Socket socket { get; set; }
        public ClientData data { get; set; }

        // holds recieved message data (without prefix). used for storing partial messages also
        public byte[] msgData { get; set; }
        public int recievedMsgPartLength { get; set; }      // last enteres byte index

        // holds recieved prefix for cases when we get only a part of prefix, and need to call Receive method one more time
        public byte[] prefixData { get; set; }
        public int recievedPrefixPartLength { get; set; }   // last enteres byte index
        public int messageLength { get; set; }              // holds recieved prefix as Integer

        // hold message to be sent
        public byte[] sendingMsg { get; set; }
        public int sendingMsgBytesSent { get; set; }    //last sent byte sequence number


        //protected AsyncUserTokenBase() : this(null, null) { }

        private readonly int _msgPrefixLength;

        protected AsyncUserTokenBase(Socket socket, ClientData data, int msgPrefixLength = 4)
        {
            _msgPrefixLength = msgPrefixLength;
            this.socket = socket;
            this.data = data;
            prefixData = new Byte[_msgPrefixLength];
            this.recievedPrefixPartLength = 0;
            this.recievedMsgPartLength = 0;
        }

        // Prepare token for reuse
        public void Clean()
        {
            msgData = null;
            sendingMsg = null;
            prefixData = new Byte[_msgPrefixLength];
            recievedMsgPartLength = 0;
            recievedPrefixPartLength = 0;
            messageLength = 0;
            sendingMsgBytesSent = 0;
        }
    }
}
