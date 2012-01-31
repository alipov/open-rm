using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using OpenRm.Common.Entities;

namespace OpenRm.Server.Gui.Modules.Monitor.Views
{
    /// <summary>
    /// Interaction logic for RemoteCommandDialogView.xaml
    /// </summary>
    public partial class RemoteCommandDialogView : Window
    {
        private Action<CustomEventArgs> _clientCallback;

        public RemoteCommandDialogView(Action<CustomEventArgs> callback)
        {
            InitializeComponent();

            _clientCallback = callback;
        }


        void okButton_Click(object sender, RoutedEventArgs e)
        {

        }


    }
}
