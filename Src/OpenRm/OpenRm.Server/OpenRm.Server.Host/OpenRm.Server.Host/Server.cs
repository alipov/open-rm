using System;
using OpenRm.Common.Entities;
using System.Configuration;

namespace OpenRm.Server.Host
{
    class Server
    {
        // these variables will be read from app.config
        public static int ListenPort;
        public static int MaxNumConnections;     //maximum number of connections
        private static string LogFilenamePattern;

        public static int ReceiveBufferSize = 64;      //recieve buffer size for tcp connection
        
        static void Main(string[] args)
        {
            // Done because exception was thrown before Main. Solution found here:
            // http://www.codeproject.com/Questions/184743/AssemblyResolve-event-not-hit
            TypeResolving.RegisterTypeResolving();

            //Main code
            StartHost();

            // End Of program
        }


        private static void StartHost()
        {
            if (ReadConfigFile())
            {
                Logger.CreateLogFile("logs", LogFilenamePattern);       // creates "logs" directory in binaries folder and set log filename
                Logger.WriteStr("Started");

                TCPServerListener srv = new TCPServerListener(ListenPort, MaxNumConnections, ReceiveBufferSize);

                Logger.WriteStr("Server terminated");
            }
        }


        // read configuration from config file 
        private static bool ReadConfigFile()
        {
            try {
                ListenPort = Int32.Parse(ConfigurationManager.AppSettings["ListenOnPort"]);
                MaxNumConnections = Int32.Parse(ConfigurationManager.AppSettings["MaxConnections"]);
                LogFilenamePattern = ConfigurationManager.AppSettings["LogFilePattern"];
            }
            catch (Exception ex) 
            {
                Logger.CriticalToEventLog("Error while reading config file: \n " + ex.Message);
                return false;
            }

            return true;   
        }
    }
}
