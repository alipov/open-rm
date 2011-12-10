using System.Windows.Controls;
using Microsoft.Practices.Prism.Regions;
using OpenRm.Server.Gui.Modules.Monitor.Api.ViewModels;
using OpenRm.Server.Gui.Modules.Monitor.Api.Views;
using OpenRm.Server.Gui.Modules.Monitor.Models;

namespace OpenRm.Server.Gui.Modules.Monitor.Views
{
    /// <summary>
    /// Interaction logic for AgentLogView.xaml
    /// </summary>
    public partial class AgentLogView : UserControl, IAgentLogView
    {
        public AgentLogView(IAgentLogViewModel viewModel)
        {
            InitializeComponent();

            this.DataContext = viewModel;

            // This view is displayed in a region with a region context.
            // The region context is defined as the currently selected agent
            // When the region context is changed, we need to propogate the
            // change to this view's view model.
            RegionContext.GetObservableContext(this).PropertyChanged += (s, e)
                                        =>
                                        viewModel.CurrentEntity =
                                        RegionContext.GetObservableContext(this).Value
                                        as AgentWrapper;
        }

        public IAgentLogViewModel ViewModel
        {
            get { return DataContext as IAgentLogViewModel; }
        }
    }
}
