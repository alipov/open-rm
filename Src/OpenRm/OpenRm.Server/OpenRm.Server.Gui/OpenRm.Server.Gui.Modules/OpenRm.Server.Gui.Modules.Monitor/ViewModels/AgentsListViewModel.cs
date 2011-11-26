using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Threading;
using Microsoft.Practices.Prism.Events;
using OpenRm.Common.Entities;
using OpenRm.Server.Gui.Inf.GuiDispatcher;
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
        IDispatcher _dispatcher;

        public AgentsListViewModel(IAgentDataService dataService, IEventAggregator eventAggregator, IDispatcher dispatcher)
        {
            _eventAggregator = eventAggregator;
            _dataService = dataService;
            AgentsCollection = new ObservableCollection<Agent>();
            _dispatcher = dispatcher;

            _eventAggregator.GetEvent<AgentsListUpdated>().Subscribe(OnAgentsListUpdated, true);
        }

        public ObservableCollection<Agent> AgentsCollection { get; set; }

        private void OnAgentsListUpdated(int id)
        {
            _dispatcher.Dispatch(DispatcherPriority.DataBind, OnAgentsListUpdatedSafe);

            #region ToDelete
            
            //if (_dispatcher.CheckAccess())
            //{
            //    foreach (var agent in _dataService.GetAgents(a => AgentsCollection.All(la => la.ID != a.ID)))
            //    {
            //        AgentsCollection.Add(agent);
            //    }
            //}
            //else
            //{

            //    _dispatcher.Invoke(DispatcherPriority.DataBind, (SendOrPostCallback) delegate
            //                            {
            //                                foreach (
            //                                    var agent in
            //                                        _dataService.
            //                                            GetAgents(
            //                                                a =>
            //                                                AgentsCollection
            //                                                    .All(
            //                                                        la
            //                                                        =>
            //                                                        la.
            //                                                            ID !=
            //                                                        a.
            //                                                            ID))
            //                                    )
            //                                {
            //                                    AgentsCollection.Add(
            //                                        agent);
            //                                }
            //                            });


            //}

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


            //foreach (var agent in _dataService.GetAgents(a => AgentsCollection.All(la => la.ID != a.ID)))
            //{
            //    AgentsCollection.Add(agent);
            //}

            #endregion
        }

        private void OnAgentsListUpdatedSafe()
        {
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
