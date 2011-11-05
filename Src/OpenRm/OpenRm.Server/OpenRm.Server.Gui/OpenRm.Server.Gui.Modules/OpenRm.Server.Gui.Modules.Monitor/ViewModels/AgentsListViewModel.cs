using System.Collections.ObjectModel;
using Microsoft.Practices.Prism.Events;
using OpenRm.Server.Gui.Modules.Monitor.Api.Services;
using OpenRm.Server.Gui.Modules.Monitor.Api.ViewModels;
using OpenRm.Server.Gui.Modules.Monitor.EventAggregatorMessages;
using OpenRm.Server.Gui.Modules.Monitor.Models;

namespace OpenRm.Server.Gui.Modules.Monitor.ViewModels
{
    public class AgentsListViewModel : IAgentsListViewModel
    {
        private readonly IEventAggregator _eventAggregator;

        public AgentsListViewModel(IAgentDataService dataService, IEventAggregator eventAggregator)
        {
            this._eventAggregator = eventAggregator;
            AgentsCollection = new ObservableCollection<Agent>();
            //{new Agent() {Name = "Alex"}, new Agent() {Name = "Michael"}};

            foreach (var agent in dataService.GetAgents(a => a != null))
            {
                AgentsCollection.Add(agent);
            }
        }

        public ObservableCollection<Agent> AgentsCollection { get; set; }


        private Agent _currentEntity;
        public Agent CurrentEntity
        {
            get { return _currentEntity; }
            set
            {
                if (value != _currentEntity)
                {
                    _currentEntity = value;

                    var agentId = value != null ? value.ID : (int?) null;

                    // Publish the EmployeeSelectedEvent event.
                    _eventAggregator.GetEvent<AgentSelectedEvent>().Publish(agentId);
                }
            }
        }
    }
}
