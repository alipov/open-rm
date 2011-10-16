using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace OpenRm.Server.Host
{
    class AsyncUserToken
    {
        public Socket socket { get; set; }
        public ClientData data { get; set; }

        // holds recieved prefix for cases when we get only a part of prefix, and need to call Receive method one more time
        //----public int[] 
        public int recievedPrefixPart = 0;
        public byte[] prefixData { get; set; }


        public AsyncUserToken() : this(null, null) { }

        public AsyncUserToken(Socket socket, ClientData data)
        {
            this.socket = socket;
            this.data = data;
        }

//TODO: can we remove it?
        public Socket Socket
        {
            get { return socket; }
            set { socket = value; }
        }
    }
}
