using System.Net;
using System.Windows.Input;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Unity;
using OpenRm.Common.Entities;
using OpenRm.Common.Entities.Network;
using OpenRm.Server.Gui.Modules.Monitor.Api.ViewModels;

namespace OpenRm.Server.Gui.Modules.Monitor.ViewModels
{
    public class AgentsRibbonTabViewModel : IAgentsRibbonTabViewModel
    {
        public AgentsRibbonTabViewModel(IUnityContainer container)
        {
            ConnectCommand = new DelegateCommand(ConnectToServer, CanConnectToServerExecute);
            _container = container;
        }

        private readonly IUnityContainer _container;
        public ICommand ConnectCommand { get; private set; }

        private void ConnectToServer()
        {
            var messageClient = _container.Resolve<IMessageClient>();
            messageClient.Connect(
                new IPEndPoint(IPAddress.Loopback, 3777),  OnConnectToServerCompleted);
        }

        private bool CanConnectToServerExecute()
        {
            var messageClient = _container.Resolve<IMessageClient>();
            return !messageClient.IsConnected;
        }

        private void OnConnectToServerCompleted(CustomEventArgs args)
        {
            
        }
    }
}
