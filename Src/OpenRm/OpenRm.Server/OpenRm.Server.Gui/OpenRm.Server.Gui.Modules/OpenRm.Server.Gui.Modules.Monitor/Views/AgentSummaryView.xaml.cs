using System.Windows.Controls;
using OpenRm.Server.Gui.Modules.Monitor.Api.ViewModels;
using OpenRm.Server.Gui.Modules.Monitor.Api.Views;

namespace OpenRm.Server.Gui.Modules.Monitor.Views
{
    /// <summary>
    /// Interaction logic for AgentSummaryView.xaml
    /// </summary>
    public partial class AgentSummaryView : UserControl, IAgentSummaryView
    {
        public AgentSummaryView(IAgentSummaryViewModel viewModel)
        {
            InitializeComponent();

            this.DataContext = viewModel;
        }

        public IAgentSummaryViewModel ViewModel
        {
            get { return DataContext as IAgentSummaryViewModel; }
        }
    }
}
