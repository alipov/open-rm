using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRm.Server.Host.Network;


namespace OpenRm.Server.Host
{
    class Server
    {
        public static int port = 3777;
        public static int maxNumConnections = 1000;     //maximum number of connections
        public static int receiveBufferSize = 512;      //recieve buffer size for tcp connection
        
        static void Main(string[] args)
        {

            TCPServerListener srv = new TCPServerListener(port, maxNumConnections, receiveBufferSize);
            



        }


    }
}
