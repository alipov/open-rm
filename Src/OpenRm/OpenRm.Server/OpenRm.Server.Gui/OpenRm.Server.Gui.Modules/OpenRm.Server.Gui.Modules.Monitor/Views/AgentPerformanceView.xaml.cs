using System.Windows.Controls;
using Microsoft.Practices.Prism.Regions;
using OpenRm.Server.Gui.Modules.Monitor.Api.ViewModels;
using OpenRm.Server.Gui.Modules.Monitor.Api.Views;
using OpenRm.Server.Gui.Modules.Monitor.Models;

namespace OpenRm.Server.Gui.Modules.Monitor.Views
{
    public partial class AgentPerformanceView : UserControl, IAgentPerformanceView
    {
        public AgentPerformanceView(IAgentPerformanceViewModel viewModel)
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

        public IAgentPerformanceViewModel ViewModel
        {
            get { return DataContext as IAgentPerformanceViewModel; }
        }
    }
}
