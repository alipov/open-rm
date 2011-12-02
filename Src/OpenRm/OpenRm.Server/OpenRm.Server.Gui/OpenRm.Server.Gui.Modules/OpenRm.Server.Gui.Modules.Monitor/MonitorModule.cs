using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using OpenRm.Common.Entities;
using OpenRm.Common.Entities.Network;
using OpenRm.Server.Gui.Inf;
using OpenRm.Server.Gui.Modules.Monitor.Api.Controllers;
using OpenRm.Server.Gui.Modules.Monitor.Api.Services;
using OpenRm.Server.Gui.Modules.Monitor.Api.ViewModels;
using OpenRm.Server.Gui.Modules.Monitor.Api.Views;
using OpenRm.Server.Gui.Modules.Monitor.Controllers;
using OpenRm.Server.Gui.Modules.Monitor.Services;
using OpenRm.Server.Gui.Modules.Monitor.ViewModels;
using OpenRm.Server.Gui.Modules.Monitor.Views;

namespace OpenRm.Server.Gui.Modules.Monitor
{
    public class MonitorModule : IModule
    {
        private readonly IUnityContainer _container;
        private readonly IRegionManager _regionManager;
        private IAgentSummaryRegionController _summaryRegionController;

        public MonitorModule(IUnityContainer container, IRegionManager regionManager)
        {
            _container = container;
            _regionManager = regionManager;
        }

        public void Initialize()
        {
            Logger.CreateLogFile("logs", "server-[date].log");

            // Register the AgentDataService concrete type with the container.
            // ContainerControlledLifetimeManager ensures singleton instance of that class.
            _container.RegisterType<IAgentDataService, AgentDataService>
                                                    (new ContainerControlledLifetimeManager());

            // ContainerControlledLifetimeManager ensures singleton instance of that class.
            _container.RegisterType<IAgentSummaryRegionController, AgentSummaryRegionController>
                                                    (new ContainerControlledLifetimeManager());

            _container.RegisterType<IMessageClient, GeneralSocketClient>
                                                    (new ContainerControlledLifetimeManager());

            _container.RegisterType<IAgentsListView, AgentsListView>();
            _container.RegisterType<IAgentsListViewModel, AgentsListViewModel>();
            _container.RegisterType<IAgentSummaryView, AgentSummaryView>();
            _container.RegisterType<IAgentSummaryViewModel, AgentSummaryViewModel>();
            _container.RegisterType<IAgentDetailsView, AgentDetailsView>();
            _container.RegisterType<IAgentDetailsViewModel, AgentDetailsViewModel>();
            _container.RegisterType<IAgentsRibbonTabView, AgentsRibbonTabView>();
            _container.RegisterType<IAgentsRibbonTabViewModel, AgentsRibbonTabViewModel>();

            // This is an example of View Discovery which associates the specified view type
            // with a region so that the view will be automatically added to the region when
            // the region is first displayed.
            _regionManager.RegisterViewWithRegion(RegionNames.RibbonRegion,
                                                  () => _container.Resolve<IAgentsRibbonTabView>());
            
            // Show the Agent List view in the shell's left hand region.
            _regionManager.RegisterViewWithRegion(RegionNames.LeftContentRegion,
                                                    () => _container.Resolve<IAgentsListView>());

            // Show the Employee Details and Employee Projects view in the tab region.
            // The tab region is defined as part of the Employee Summary view which is only
            // displayed once the user has selected an employee in the Employee List view.
            _regionManager.RegisterViewWithRegion(RegionNames.TabRegion,
                                                       () => _container.Resolve<IAgentDetailsView>());

            // Create the summary region controller.
            // This is used to programmatically (using injection) coordinate the view in the summary region 
            // of the shell.
            _summaryRegionController = _container.Resolve<IAgentSummaryRegionController>();
        }
    }
}
