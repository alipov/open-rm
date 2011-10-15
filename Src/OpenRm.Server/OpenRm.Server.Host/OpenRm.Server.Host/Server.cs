using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace OpenRm.Server.Host
{
    class Server
    {
        private static string logFilenamePattern = "server-<date>.log";
        private static Logger log;

        public static int port = 3777;
        public static int maxNumConnections = 1000;     //maximum number of connections
        public static int receiveBufferSize = 512;      //recieve buffer size for tcp connection
        
        static void Main(string[] args)
        {
            log = new Logger(logFilenamePattern);
            log.WriteStr("Started");
            
            //TCPServerListener srv = new TCPServerListener(port, maxNumConnections, receiveBufferSize, log);



            log.Close();
        }



    }
}
