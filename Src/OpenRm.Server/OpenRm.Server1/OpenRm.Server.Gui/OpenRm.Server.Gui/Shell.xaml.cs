using System;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Xml;
using wox.serial;

namespace OpenRm.Server.Gui
{
    /// <summary>
    /// Interaction logic for Shell.xaml
    /// </summary>
    public partial class Shell : Window
    {
        public Shell()
        {
            InitializeComponent();

            // TODO: remove it once done testing
            TcpClient client = null;
            try
            {
                client = new TcpClient(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0));
            }
            catch (Exception e)
            {
                return;
            }
            client.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9050));
            var emp = new Emp() {Age = 5, Name = "Alex"};
            var ns = client.GetStream();
            var writer = XmlWriter.Create(ns);
            Easy.save(emp, writer);
        }
    }

    public class Emp
    {
        public string Name;
        public  int Age;
    }
}
