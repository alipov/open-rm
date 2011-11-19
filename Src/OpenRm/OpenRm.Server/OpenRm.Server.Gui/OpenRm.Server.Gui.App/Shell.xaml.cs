using System.Windows;

namespace OpenRm.Server.Gui
{
    /// <summary>
    /// Interaction logic for Shell.xaml
    /// </summary>
    public partial class Shell : Window
    {
        public Shell()
        {
#if DEBUG
            BindingErrorTraceListener.SetTrace();
#endif
            InitializeComponent();
        }
    }
}
