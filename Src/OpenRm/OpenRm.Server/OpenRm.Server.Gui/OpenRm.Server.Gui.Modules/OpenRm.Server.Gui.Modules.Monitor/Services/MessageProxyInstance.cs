using System;
using System.Net;
using System.Windows.Threading;
using Microsoft.Practices.Unity;
using OpenRm.Common.Entities;
using OpenRm.Common.Entities.Network;
using OpenRm.Common.Entities.Network.Messages;
using OpenRm.Server.Gui.Inf.GuiDispatcher;

namespace OpenRm.Server.Gui.Modules.Monitor.Services
{
    public class MessageProxyInstance : IMessageProxyInstance
    {
        private readonly IUnityContainer _container;

        public MessageProxyInstance(IUnityContainer container)
        {
            _container = container;
            Callback = OnProxyReceiveCompleted;
        }

        public Message Message { get; private set; }
        public Action<CustomEventArgs> Callback { get; private set; }

        private Action<CustomEventArgs> _clientCallback;
        private object _stateObject;

        public void Send(Message message, Action<CustomEventArgs> callback, object stateObject = null)
        {
            Message = message;
            _clientCallback = callback;
            _stateObject = stateObject;

            var service = _container.Resolve<MessageProxyService>();

            service.Send(this);
        }

        private void OnProxyReceiveCompleted(CustomEventArgs args)
        {
            var dispatcher = _container.Resolve<IDispatcher>();

            dispatcher.Dispatch(
                DispatcherPriority.DataBind,
                () =>
                    {
                        if(_clientCallback != null)
                            _clientCallback.Invoke(args);
                    }
                );
        }
    }
}