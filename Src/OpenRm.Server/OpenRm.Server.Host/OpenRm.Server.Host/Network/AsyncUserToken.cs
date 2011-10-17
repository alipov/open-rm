using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace OpenRm.Server.Host
{
    class AsyncUserToken
    {
        public Socket socket;
        public ClientData data;

        // holds recieved message data (without prefix). used for storing partial messages also
        public byte[] msgData;
        public int recievedMsgPartLength;

        // holds recieved prefix for cases when we get only a part of prefix, and need to call Receive method one more time
        public byte[] prefixData;
        public int recievedPrefixPartLength;

        public int msgStartOffset;  
        

        public AsyncUserToken() : this(null, null) { }

        public AsyncUserToken(Socket socket, ClientData data)
        {
            this.socket = socket;
            this.data = data;
            prefixData = new Byte[TCPServerListener.msgPrefixLength];       // 4 bytes prefix
            this.recievedPrefixPartLength = 0;
            this.recievedMsgPartLength = 0;
            this.msgStartOffset = 0;
        }

//TODO: can we remove it?
        public Socket Socket
        {
            get { return socket; }
            set { socket = value; }
        }
    }
}
