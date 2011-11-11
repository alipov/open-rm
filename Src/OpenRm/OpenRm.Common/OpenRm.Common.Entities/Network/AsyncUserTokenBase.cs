using System;
using System.Net.Sockets;

namespace OpenRm.Common.Entities.Network
{
    public abstract class AsyncUserTokenBase
    {
        public Socket Socket { get; set; }
        public ClientData Data { get; set; }

        // holds recieved message data (without prefix). used for storing partial messages also
        public byte[] MsgData { get; set; }
        public int RecievedMsgPartLength { get; set; }      // last enteres byte index

        // holds recieved prefix for cases when we get only a part of prefix, and need to call Receive method one more time
        public byte[] PrefixData { get; set; }
        public int RecievedPrefixPartLength { get; set; }   // last enteres byte index
        public int MessageLength { get; set; }              // holds recieved prefix as Integer

        // hold message to be sent
        public byte[] SendingMsg { get; set; }
        public int SendingMsgBytesSent { get; set; }    //last sent byte sequence number


        //protected AsyncUserTokenBase() : this(null, null) { }

        private readonly int _msgPrefixLength;

        protected AsyncUserTokenBase(Socket socket, ClientData data, int msgPrefixLength = 4)
        {
            _msgPrefixLength = msgPrefixLength;
            Socket = socket;
            Data = data;
            PrefixData = new Byte[_msgPrefixLength];
            RecievedPrefixPartLength = 0;
            RecievedMsgPartLength = 0;
        }

        // Prepare token for reuse
        public void Clean()
        {
            MsgData = null;
            SendingMsg = null;
            PrefixData = new Byte[_msgPrefixLength];
            RecievedMsgPartLength = 0;
            RecievedPrefixPartLength = 0;
            MessageLength = 0;
            SendingMsgBytesSent = 0;
        }
    }
}
