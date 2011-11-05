using System.Windows.Controls;
using OpenRm.Server.Gui.Modules.Monitor.Api.ViewModels;
using OpenRm.Server.Gui.Modules.Monitor.Api.Views;

namespace OpenRm.Server.Gui.Modules.Monitor.Views
{
    /// <summary>
    /// Interaction logic for AgentDetailsView.xaml
    /// </summary>
    public partial class AgentDetailsView : UserControl, IAgentDetailsView
    {
        public AgentDetailsView(IAgentDetailsViewModel viewModel)
        {
            InitializeComponent();

            this.DataContext = viewModel;
        }
    }
}
