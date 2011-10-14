using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml;
using wox.serial;

namespace OpenRm.Server.Host
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = new TcpListener(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9050));
            server.Start();

            TcpClient client = server.AcceptTcpClient();
            NetworkStream channel = client.GetStream();
            var rdr = XmlReader.Create(channel);
            Easy.load(rdr);
        }
    }

    public class Emp
    {
        public string Name;
        public int Age;
    }
}
