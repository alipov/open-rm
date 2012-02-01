using System;
using OpenRm.Common.Entities;
using OpenRm.Common.Entities.Network;
using OpenRm.Common.Entities.Network.Messages;

namespace OpenRm.Server.Host.Network
{
    public interface IMessageServer
    {
        void Start(Action<HostCustomEventArgs> receiveCallback);

        void Send(Message message, HostAsyncUserToken token);
    }
}
