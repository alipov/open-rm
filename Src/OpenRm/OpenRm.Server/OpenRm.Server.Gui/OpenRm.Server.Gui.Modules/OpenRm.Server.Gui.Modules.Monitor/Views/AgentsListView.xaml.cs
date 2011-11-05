using System.Windows.Controls;
using OpenRm.Server.Gui.Modules.Monitor.Api.ViewModels;
using OpenRm.Server.Gui.Modules.Monitor.Api.Views;

namespace OpenRm.Server.Gui.Modules.Monitor.Views
{
    /// <summary>
    /// Interaction logic for AgentsListView.xaml
    /// </summary>
    public partial class AgentsListView : UserControl, IAgentsListView
    {
        public AgentsListView(IAgentsListViewModel viewModel)
        {
            
            InitializeComponent();

            this.DataContext = viewModel;
        }
    }
}
