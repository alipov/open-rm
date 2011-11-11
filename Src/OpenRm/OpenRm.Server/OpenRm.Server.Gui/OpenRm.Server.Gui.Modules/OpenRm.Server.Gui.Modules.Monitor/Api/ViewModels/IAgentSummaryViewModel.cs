using OpenRm.Common.Entities;
using OpenRm.Server.Gui.Inf.Api.Mvvm;

namespace OpenRm.Server.Gui.Modules.Monitor.Api.ViewModels
{
    public interface IAgentSummaryViewModel : IViewModel
    {
        Agent CurrentEntity { get; set; }
    }
}
