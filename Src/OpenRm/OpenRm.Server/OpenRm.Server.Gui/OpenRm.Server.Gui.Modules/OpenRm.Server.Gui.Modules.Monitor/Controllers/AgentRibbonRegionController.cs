using System;
using System.Linq;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using OpenRm.Common.Entities;
using OpenRm.Server.Gui.Inf;
using OpenRm.Server.Gui.Modules.Monitor.Api.Controllers;
using OpenRm.Server.Gui.Modules.Monitor.Api.Services;
using OpenRm.Server.Gui.Modules.Monitor.Api.Views;
using OpenRm.Server.Gui.Modules.Monitor.EventAggregatorMessages;
using OpenRm.Server.Gui.Modules.Monitor.Views;

namespace OpenRm.Server.Gui.Modules.Monitor.Controllers
{
    internal class AgentRibbonRegionController : IAgentRibbonRegionController
    {
        private readonly IUnityContainer _container;
        private readonly IRegionManager _regionManager;
        private readonly IEventAggregator _eventAggregator;
        private readonly IAgentDataService _dataService;

        public AgentRibbonRegionController(IUnityContainer container, 
                                            IRegionManager regionManager, 
                                            IEventAggregator eventAggregator, 
                                            IAgentDataService dataService)
        {
            if (container == null) throw new ArgumentNullException("container");
            if (regionManager == null) throw new ArgumentNullException("regionManager");
            if (eventAggregator == null) throw new ArgumentNullException("eventAggregator");
            if (dataService == null) throw new ArgumentNullException("dataService");

            _container = container;
            _regionManager = regionManager;
            _eventAggregator = eventAggregator;
            _dataService = dataService;

            // Subscribe to the EmployeeSelectedEvent event.
            // This event is fired whenever the user selects an
            // employee in the EmployeeListView.
            _eventAggregator.GetEvent<AgentSelectedEvent>().Subscribe(OnAgentSelected, true);
        }

        private void OnAgentSelected(int? agentId)
        {
            Agent selectedAgent = null;
            // Get the agent entity with the selected ID.
            if(agentId.HasValue)
            {
                selectedAgent = _dataService.GetAgents(a => a.ID == agentId.Value).SingleOrDefault();
            }

            // Get a reference to the main region.
            IRegion ribbonRegion = _regionManager.Regions[RegionNames.RibbonRegion];
            if (ribbonRegion == null) return;
            
            // Check to see if we need to create an instance of the view.
            var view = ribbonRegion.Views.SingleOrDefault() as IAgentsRibbonTabView;

            // Set the current agent property on the view model.
            if (view != null && view.ViewModel != null)
            {
                view.ViewModel.CurrentEntity = selectedAgent;
            }
        }
    }
}
