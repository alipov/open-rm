using System;
using System.Windows;
using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Prism.UnityExtensions;
using Microsoft.Practices.Unity;

namespace OpenRm.Server.Gui
{
    public class Bootstrapper : UnityBootstrapper
    {
        protected override DependencyObject CreateShell()
        {
            return Container.Resolve<Shell>();
        }

        protected override void InitializeShell()
        {
            base.InitializeShell();

            Application.Current.MainWindow = (Window)Shell;
            Application.Current.MainWindow.Show();
        }

        protected override IModuleCatalog CreateModuleCatalog()
        {
            var moduleCatalog =
                Microsoft.Practices.Prism.Modularity.ModuleCatalog.CreateFromXaml
                    (new Uri("/OpenRm.Server.Gui;component/XamlCatalog.xaml", UriKind.Relative));

            return moduleCatalog;
        }
    }
}
