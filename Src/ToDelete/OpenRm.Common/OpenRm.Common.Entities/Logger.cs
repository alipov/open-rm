using System;
using System.IO;
using OpenRm.Common.Entities;

namespace OpenRm.Server.Host
{
    public static class Logger //: IDisposable
    {
        // TODO: 1. in Main - read the log directory from app.config + call CreateLogDirectory method
        //       2. each time we want to write log - execute the WriteStr method

        private static string _logFile; //= "asdf";
        private static string _logDirectory = "";
        //public static string logFilenamePattern; //= "server-<date>.log";
        //private string logDirectory = "logs";
        //private StreamWriter log;

        private static readonly object lck = new object();     // for handeling Writes from many threads

        //public Logger(string logFilenamePattern)
        //{
        //    //replace "<date>" by current date and time
        //    string logFilename = logFilenamePattern.Replace("<date>", DateTime.Now.ToString("ddMMyy-HHmmss"));
        //    try
        //    {
        //        //CreateLogDirectory();       //check if Logs directory exist (otherwise creates it)
        //        //this.log = new StreamWriter(logDirectory + "\\" + logFilename);        //create and open file for writing
        //    }
        //    catch (IOException err)
        //    {
        //        //throw new MyException(err); 
        //        //TODO:  show popup? ignore?
        //        Console.WriteLine("ERROR while opening log file for writing: " + err.Message);
        //    }

        //}

        public static void CreateLogFile(string logDirectory, string logPattern)
        {
            if (!Directory.Exists(logDirectory))
                Directory.CreateDirectory(logDirectory);
            //logFilenamePattern = logPattern;
            _logDirectory = logDirectory;
            _logFile = logPattern.Replace("<date>", DateTime.Now.ToString("ddMMyy-HHmmss"));
        }

        public static void WriteStr(string str)
        {
            lock (lck)
            {
                using (var log = new StreamWriter(_logDirectory + "\\" + _logFile))
                {
                    log.WriteLine(DateTime.Now.ToString("dd.MM HH:mm:ss") + " | " + str);
                    //log.Flush();
                }
            }
        }

        //public void Close()
        //{
        //    log.Close();
        //}


        //private bool _disposed;

        //~Logger()
        //{
        //    Dispose(false);
        //}

        //public void Dispose()
        //{
        //    Dispose(true);
        //    GC.SuppressFinalize(this);
        //}

        //private void Dispose(bool disposing)
        //{
        //    if (!_disposed)
        //    {
        //        if (disposing)
        //        {

        //        }
        //        //log.Close();
        //    }
        //    _disposed = true;
        //}
    }
}