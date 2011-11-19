using System.Windows;
using Microsoft.Windows.Controls.Ribbon;
using OpenRm.Server.Gui.Modules.Monitor.Api.ViewModels;
using OpenRm.Server.Gui.Modules.Monitor.Api.Views;

namespace OpenRm.Server.Gui.Modules.Monitor.Views
{
    /// <summary>
    /// Interaction logic for AgentsRibbonTabView.xaml
    /// </summary>
    public partial class AgentsRibbonTabView : RibbonTab, IAgentsRibbonTabView
    {
        public AgentsRibbonTabView(IAgentsRibbonTabViewModel viewModel)
        {
            InitializeComponent();

            this.DataContext = viewModel;
        }
    }
}
