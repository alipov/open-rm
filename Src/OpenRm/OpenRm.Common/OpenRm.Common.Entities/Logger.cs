using System;
using System.IO;
using OpenRm.Common.Entities;

namespace OpenRm.Server.Host
{
    public static class Logger
    {
        // TODO: 1. in Main - read the log directory from app.config + call CreateLogDirectory method
        //       2. each time we want to write log - execute the WriteStr method

        private static string _logFile; 
        private static string _logDirectory;

        private static readonly object lck = new object();     // for handeling Writes from many threads

        public static void CreateLogFile(string logDirectory, string logPattern)
        {
            if (!Directory.Exists(logDirectory))
                Directory.CreateDirectory(logDirectory);
            _logDirectory = logDirectory;
            _logFile = logPattern.Replace("<date>", DateTime.Now.ToString("ddMMyy-HHmmss"));
        }

        public static void WriteStr(string str)
        {
            lock (lck)
            {
                using (var log = new StreamWriter(_logDirectory + "\\" + _logFile, true))
                {
                    log.WriteLine(DateTime.Now.ToString("dd.MM HH:mm:ss") + " | " + str);
                }
            }
        }

    }
}