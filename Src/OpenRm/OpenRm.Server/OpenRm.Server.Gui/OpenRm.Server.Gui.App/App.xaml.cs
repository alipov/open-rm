using System.Windows;

namespace OpenRm.Server.Gui
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App() : base()
        {
            TypeResolving.RegisterTypeResolving();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // create the bootstrapper
            var bootstrapper = new Bootstrapper();
            bootstrapper.Run();
        }
    }
}
