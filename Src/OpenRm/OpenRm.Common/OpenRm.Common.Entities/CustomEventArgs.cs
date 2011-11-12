using System;
using System.Net.Sockets;
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
    }
}
