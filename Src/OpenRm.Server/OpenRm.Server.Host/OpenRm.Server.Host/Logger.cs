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

        public Logger(string logFilenamePattern)
        {

            string logFilename = logFilenamePattern.Replace("<date>", DateTime.Now.ToString("ddMMyy-HHmmss"));     //replace "<date>" by current date and time
            try
            {
                CreateLogDirectory();
                log = new StreamWriter(logDirectory + "\\" + logFilename);        //create and open file for writing
            }
            catch (IOException err) 
            {
//TODO:  show popup? ignore?
                Console.WriteLine(err.Message);
            }
            
        }

        private void CreateLogDirectory()
        {
            if (!Directory.Exists(logDirectory))
                Directory.CreateDirectory(logDirectory);
        }

        public void WriteLine(string str)
        {
            lock (log)
            {
                log.WriteLine(str);
            }
        }

        public void Close()
        {
            log.Close();
        }


    }
}
