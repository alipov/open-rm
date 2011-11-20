using System;
using System.Net;
using System.Windows.Input;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Unity;
using OpenRm.Common.Entities;
using OpenRm.Common.Entities.Network;
using OpenRm.Common.Entities.Network.Messages;
using OpenRm.Server.Gui.Modules.Monitor.Api.Services;
using OpenRm.Server.Gui.Modules.Monitor.Api.ViewModels;
using OpenRm.Server.Gui.Modules.Monitor.EventAggregatorMessages;

namespace OpenRm.Server.Gui.Modules.Monitor.ViewModels
{
    public class AgentsRibbonTabViewModel : IAgentsRibbonTabViewModel
    {
        private IAgentDataService _dataService;
        
        public AgentsRibbonTabViewModel(IUnityContainer container, IAgentDataService dataService)
        {
            ConnectCommand = new DelegateCommand(ConnectToServer, CanConnectToServerExecute);
            RefreshAgentsCommand = new DelegateCommand(RefreshAgentsList);
            _container = container;
            _dataService = dataService;
        }

        private readonly IUnityContainer _container;
        public ICommand ConnectCommand { get; private set; }
        public ICommand RefreshAgentsCommand { get; private set; }

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

        private void RefreshAgentsList()
        {
            var messageClient = _container.Resolve<IMessageClient>();
            if(messageClient.IsConnected == false)
                throw new InvalidOperationException("Client is disconnected from the host.");

            var listAgentsMessage = new RequestMessage()
                                     {
                                         Request = new ListAgentsRequest()
                                     };

            messageClient.Send(listAgentsMessage, OnRefreshAgentsListCompleted);
        }

        private void OnRefreshAgentsListCompleted(CustomEventArgs args)
        {
            var message = (ResponseMessage) args.Result;
            var response = (ListAgentsResponse) message.Response;

            _dataService.SetAgents(response.Agents);
        }
    }
}
