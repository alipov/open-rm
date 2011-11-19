using System.Windows.Controls;
using OpenRm.Server.Gui.Modules.Monitor.Api.ViewModels;
using OpenRm.Server.Gui.Modules.Monitor.Api.Views;

namespace OpenRm.Server.Gui.Modules.Monitor.Views
{
    /// <summary>
    /// Interaction logic for Toolbar.xaml
    /// </summary>
    public partial class ToolbarView : UserControl, IToolbarView
    {
        public ToolbarView(IToolbarViewModel viewModel)
        {
            InitializeComponent();

            DataContext = viewModel;
        }

        public IToolbarViewModel ViewModel
        {
            get { return DataContext as IToolbarViewModel; }
        }
    }
}
