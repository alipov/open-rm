using OpenRm.Server.Gui.Inf.Api.Mvvm;
using OpenRm.Server.Gui.Modules.Monitor.Api.ViewModels;

namespace OpenRm.Server.Gui.Modules.Monitor.Api.Views
{
    public interface IAgentsRibbonTabView : IView
    {
        IAgentsRibbonTabViewModel ViewModel { get; }
    }
}
