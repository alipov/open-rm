using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace OpenRm.Server.Host
{
    class Server
    {
        private const string LogFilenamePattern = "server-<date>.log";
        //private static Logger log;

        public static int Port = 3777;
        public static int MaxNumConnections = 1000;     //maximum number of connections
        public static int ReceiveBufferSize = 16;      //recieve buffer size for tcp connection
        
        static void Main(string[] args)
        {
            //TODO: get directory from app.config
            //System.Configuration.
            Logger.CreateLogFile("logs", LogFilenamePattern);

            //log = new Logger(logFilenamePattern);
            Logger.WriteStr("Started");
            //log.WriteStr("Started");

            try
            {
                TCPServerListener srv = new TCPServerListener(Port, MaxNumConnections, ReceiveBufferSize);
            }
            catch (Exception ex)
            {
                Logger.WriteStr("ERROR: Exception: " + ex.Message + ". Trace: " + ex.StackTrace);
            }
        }
    }
}
