using System;
using System.Collections.Generic;
using Microsoft.Practices.Unity;
using Microsoft.Windows.Controls;
using OpenRm.Common.Entities;
using OpenRm.Common.Entities.Network;

namespace OpenRm.Server.Gui.Modules.Monitor.Services
{
    public class MessageProxyService
    {
        private readonly IUnityContainer _container;
        private readonly Dictionary<Guid, IMessageProxyInstance> _instances;

        public MessageProxyService(IUnityContainer container)
        {
            _container = container;
            _instances = new Dictionary<Guid, IMessageProxyInstance>();
        }

        public Guid Send(IMessageProxyInstance instance)
        {
            var guid = Guid.Empty;

            if (instance != null && instance.Message != null)
            { 
                guid = Guid.NewGuid();
                _instances.Add(guid, instance);

                instance.Message.UniqueID = guid.ToString();

                var messageClient = _container.Resolve<IMessageClient>();
                
                if(messageClient.IsConnected == false)
                {
                    throw new InvalidOperationException("Client is disconnected from the host.");
                }

                messageClient.Send(instance.Message, ProxyCallback);
            }
            return guid;
        }

        private void ProxyCallback(CustomEventArgs args)
        {
            if (args != null && args.Result != null)
            {
                Guid guid;
                var isSucceed = Guid.TryParse(args.Result.UniqueID, out guid);
                if (isSucceed)
                {
                    IMessageProxyInstance instance;
                    _instances.TryGetValue(guid, out instance);
                    _instances.Remove(guid);

                    if (instance != null && instance.Callback != null)
                    {
                        instance.Callback.Invoke(args);
                    }
                }
            }
        }
    }
}
