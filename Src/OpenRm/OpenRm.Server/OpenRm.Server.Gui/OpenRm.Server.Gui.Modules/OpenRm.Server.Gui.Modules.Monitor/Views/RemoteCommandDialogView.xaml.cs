using System;
using System.Windows;
using OpenRm.Common.Entities;

namespace OpenRm.Server.Gui.Modules.Monitor.Views
{
    /// <summary>
    /// Interaction logic for RemoteCommandDialogView.xaml
    /// </summary>
    public partial class RemoteCommandDialogView : Window
    {
        //private Action<CustomEventArgs> _clientCallback;

        public RemoteCommandDialogView()
        {
            InitializeComponent();

            //_clientCallback = callback;
        }


        void okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }


    }
}
