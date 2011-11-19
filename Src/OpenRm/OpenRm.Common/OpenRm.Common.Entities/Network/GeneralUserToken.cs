using System;
using System.Net.Sockets;

namespace OpenRm.Common.Entities.Network
{
    public class GeneralUserToken : AsyncUserTokenBase
    {
        //public GeneralUserToken() : base(null, null) { }
        private int _msgPrefixLength;
        public Action<CustomEventArgs> Callback { get; set; }
        public Socket Socket { get; set; }

        public GeneralUserToken(Socket socket, int msgPrefixLength = 4)
            : this(socket, null, msgPrefixLength){ }

        public GeneralUserToken(Socket socket, Action<CustomEventArgs> callback, int msgPrefixLength = 4)
            : base(socket, null, msgPrefixLength)
        {
            Socket = socket;
            _msgPrefixLength = msgPrefixLength;
            Callback = callback;
        }
    }
}
