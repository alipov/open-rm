using Microsoft.Windows.Controls.Ribbon;

namespace OpenRm.Server.Gui
{
    /// <summary>
    /// Interaction logic for ShellRibbon.xaml
    /// </summary>
    public partial class ShellRibbon : RibbonWindow
    {
        public ShellRibbon()
        {
#if DEBUG
            BindingErrorTraceListener.SetTrace();
#endif
            InitializeComponent();

            // Insert code required on object creation below this point.
        }
    }
}
