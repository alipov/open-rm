using System;
using OpenRm.Common.Entities.Network.Messages;

namespace OpenRm.Common.Entities.Network
{
    public interface IMessageProxyInstance
    {
        Message Message { get; }
        Action<CustomEventArgs> Callback { get; }

        void Send(Message message, Action<CustomEventArgs> callback, object stateObject = null);
    }
}
