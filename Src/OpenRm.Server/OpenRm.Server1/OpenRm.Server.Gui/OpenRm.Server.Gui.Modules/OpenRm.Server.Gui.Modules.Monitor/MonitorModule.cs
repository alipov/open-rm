using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using OpenRm.Server.Gui.Inf;

namespace OpenRm.Server.Gui.Modules.Monitor
{
    public class MonitorModule : IModule
    {
        private IUnityContainer _container;
        private IRegionManager _regionManager;

        public MonitorModule(IUnityContainer container, IRegionManager regionManager)
        {
            _container = container;
            _regionManager = regionManager;
        }

        public void Initialize()
        {
            _regionManager.RegisterViewWithRegion(RegionNames.ToolbarRegion, typeof (ToolbarView));
            _regionManager.RegisterViewWithRegion(RegionNames.ContentRegion, typeof (ContentView));
        }
    }
}
