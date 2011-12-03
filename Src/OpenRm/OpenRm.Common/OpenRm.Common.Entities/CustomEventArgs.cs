using System;
using System.Net;
using System.Net.Sockets;
using OpenRm.Common.Entities.Network;
using OpenRm.Common.Entities.Network.Messages;

namespace OpenRm.Common.Entities
{
    public class CustomEventArgs : EventArgs
    {
        public CustomEventArgs(SocketError status, Message result)
        {
            Status = status;
            Result = result;
        }

        public SocketError Status { get; private set; }
        public Message Result { get; private set; }
        public IPEndPoint LocalEndPoint { get; set; }
    }

    public class HostCustomEventArgs : CustomEventArgs
    {
        public HostCustomEventArgs(SocketError status, Message result) : base(status, result)
        {
        }

        public HostAsyncUserToken Token { get; set; }
        
    }
}
