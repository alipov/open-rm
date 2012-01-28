using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;

namespace OpenRm.Common.Entities
{
    public static class SettingsManager
    {

        public static IPEndPoint ServerEndPoint { get; set; }          // Server IP and Port. (got from configuration file)
        public static string LogFilenamePattern { get; set; }         // Log filename (got from configuration file)
        public static string SecretKey { get; set; }                 // Secret key for encryption (got from configuration file)

        // read configuration from config file
        public static bool ReadConfigFile()
        {
            try
            {
                var ip = ConfigurationManager.AppSettings["ServerIP"];
                var port = Int32.Parse(ConfigurationManager.AppSettings["ServerPort"]);
                ServerEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
                LogFilenamePattern = ConfigurationManager.AppSettings["LogFilePattern"];
                SecretKey = ConfigurationManager.AppSettings["Secret"];
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
