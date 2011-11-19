using System.Net;
using System.Windows.Input;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Unity;
using OpenRm.Common.Entities;
using OpenRm.Common.Entities.Network;
using OpenRm.Server.Gui.Modules.Monitor.Api.ViewModels;

namespace OpenRm.Server.Gui.Modules.Monitor.ViewModels
{
    public class ToolbarViewModel : IToolbarViewModel
    {
        public ToolbarViewModel(IUnityContainer container)
        {
            ConnectCommand = new DelegateCommand(ConnectToServer);
            _container = container;
        }

        private IUnityContainer _container;
        public ICommand ConnectCommand { get; private set; }

        private void ConnectToServer()
        {
            var messageClient = _container.Resolve<IMessageClient>();
            messageClient.Connect(
                new IPEndPoint(IPAddress.Loopback, 3777),  OnConnectToServerCompleted);
        }

        private void OnConnectToServerCompleted(CustomEventArgs args)
        {
            
        }
    }
}
