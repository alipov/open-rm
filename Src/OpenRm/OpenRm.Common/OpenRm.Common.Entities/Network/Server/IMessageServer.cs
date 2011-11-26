using System;
using OpenRm.Common.Entities.Network.Messages;

namespace OpenRm.Common.Entities.Network.Server
{
    public interface IMessageServer
    {
        void Start(Action<HostCustomEventArgs> receiveCallback);

        void Send(Message message, HostAsyncUserToken token);
    }
}
