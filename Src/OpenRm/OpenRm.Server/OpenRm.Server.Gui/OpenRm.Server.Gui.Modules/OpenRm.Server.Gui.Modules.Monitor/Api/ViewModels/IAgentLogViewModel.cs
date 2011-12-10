using OpenRm.Server.Gui.Inf.Api.Mvvm;
using OpenRm.Server.Gui.Modules.Monitor.Models;

namespace OpenRm.Server.Gui.Modules.Monitor.Api.ViewModels
{
    public interface IAgentLogViewModel : IViewModel
    {
        AgentWrapper CurrentEntity { get; set; }
    }
}
