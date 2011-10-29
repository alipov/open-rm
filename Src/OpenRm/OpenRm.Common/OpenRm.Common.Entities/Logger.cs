using System;
using System.IO;
using OpenRm.Common.Entities;
using System.Diagnostics;

namespace OpenRm.Common.Entities
{
    public static class Logger
    {

        private static string _logFile; 
        private static string _logDirectory;

        private static readonly object lck = new object();     // for handeling Writes from many threads

        public static void CreateLogFile(string logDirectory, string logPattern)
        {
            if (!Directory.Exists(logDirectory))
                Directory.CreateDirectory(logDirectory);
            _logDirectory = logDirectory;
            _logFile = logPattern.Replace("[date]", DateTime.Now.ToString("ddMMyy-HHmmss"));
        }

        public static void WriteStr(string str)
        {
            lock (lck)
            {
                try     // probably Antivirus can cause an error...
                {
                    using (var log = new StreamWriter(_logDirectory + "\\" + _logFile, true))
                    {
                        log.WriteLine(DateTime.Now.ToString("dd.MM HH:mm:ss") + " | " + str);
                    }
                }
                catch (Exception ex) 
                {
                    Console.WriteLine("Error while writing to log: " + str + ". Exception: " + ex.ToString() + ". But we have to continue...");
                }
            }
        }

        // writes critical event in Application EventLog
        public static void CriticalToEventLog(string str)
        {
            string sSource = System.Diagnostics.Process.GetCurrentProcess().ProcessName;

            if (!EventLog.SourceExists(sSource))
                EventLog.CreateEventSource(sSource, "Application");

            EventLog.WriteEntry(sSource, str, EventLogEntryType.Error);
        }

    }
}
