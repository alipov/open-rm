using System;
using System.Net;
using OpenRm.Common.Entities.Network.Messages;

namespace OpenRm.Common.Entities.Network
{
    public interface IMessageClient
    {
        void Connect(IPEndPoint endPoint, Action<CustomEventArgs> callback);
        void Send(Message message, Action<CustomEventArgs> callback);
        void Disconnect(Action<CustomEventArgs> callback);
    }
}
