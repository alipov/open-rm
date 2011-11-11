using OpenRm.Server.Gui.Modules.Monitor.Api.ViewModels;

namespace OpenRm.Server.Gui.Modules.Monitor.ViewModels
{
    public class AgentDetailsViewModel : IAgentDetailsViewModel
    {
        // http://compositewpf.codeplex.com/discussions/30673
        private const string _title = "Module B";
        public string HeaderInfo { get { return _title; } }
    }
}
