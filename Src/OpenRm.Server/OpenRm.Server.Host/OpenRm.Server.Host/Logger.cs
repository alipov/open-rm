using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace OpenRm.Server.Host
{
    class Logger
    {
        private string logDirectory = "logs";
        private StreamWriter log;

        private readonly object lck = new object();     // for handeling Writes from many threads

        public Logger(string logFilenamePattern)
        {
            //replace "<date>" by current date and time
            string logFilename = logFilenamePattern.Replace("<date>", DateTime.Now.ToString("ddMMyy-HHmmss"));     
            try
            {
                CreateLogDirectory();       //check if Logs directory exist (otherwise creates it)
                this.log = new StreamWriter(logDirectory + "\\" + logFilename);        //create and open file for writing
            }
            catch (IOException err) 
            {
//TODO:  show popup? ignore?
Console.WriteLine("ERROR while opening log file for writing: " + err.Message);
            }
            
        }

        private void CreateLogDirectory()
        {
            if (!Directory.Exists(logDirectory))
                Directory.CreateDirectory(logDirectory);
        }

        public void WriteStr(string str)
        {
            lock (this.lck)
            {
                log.WriteLine(DateTime.Now.ToString("dd.MM HH:mm:ss") + " | " + str);
                log.Flush();
            }
        }

        public void Close()
        {
            log.Close();
        }


    }
}
