using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Threading;
using Microsoft.Practices.Prism.Events;
using OpenRm.Common.Entities;
using OpenRm.Server.Gui.Modules.Monitor.Api.Services;
using OpenRm.Server.Gui.Modules.Monitor.Api.ViewModels;
using OpenRm.Server.Gui.Modules.Monitor.EventAggregatorMessages;

namespace OpenRm.Server.Gui.Modules.Monitor.ViewModels
{
    public class AgentsListViewModel : IAgentsListViewModel
    {
        private readonly IEventAggregator _eventAggregator;
        private IAgentDataService _dataService;
        public delegate void MethodInvoker();

        public AgentsListViewModel(IAgentDataService dataService, IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
            _dataService = dataService;
            AgentsCollection = new ObservableCollection<Agent>();
            //{new Agent() {Name = "Alex"}, new Agent() {Name = "Michael"}};

            //foreach (var agent in dataService.GetAgents(a => a != null))
            //{
            //    AgentsCollection.Add(agent);
            //}
            _eventAggregator.GetEvent<AgentsListUpdated>().Subscribe(OnAgentsListUpdated, true);
        }

        public ObservableCollection<Agent> AgentsCollection { get; set; }

        private void OnAgentsListUpdated(int id)
        {
            //if (UIThread != null)
            //{
                
            //    Dispatcher.FromThread(UIThread).Invoke((MethodInvoker)delegate
            //    {
            //        queriesViewModel.AddQuery(info);
            //    }
            //    , null);
            //}
            //else
            //{
            //    queriesViewModel.AddQuery(info);
            //} 


            foreach (var agent in _dataService.GetAgents(a => AgentsCollection.All(la => la.ID != a.ID)))
            {
                AgentsCollection.Add(agent);
            }
        }

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
