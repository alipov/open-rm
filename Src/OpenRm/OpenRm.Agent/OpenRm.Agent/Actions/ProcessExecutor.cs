//using System;
//using System.Diagnostics;
//using System.Threading;
//using OpenRm.Common.Entities.Network.Messages;

//namespace OpenRm.Agent.Actions
//{
//    class ProcessExecutor
//    {
//        // Runs executable provided by "proc" parameter, which also holds information how this process should be executed
//        public static ResponseBase Run(RunProcessRequest proc)
//        {
//            RunProcessResponse status = new RunProcessResponse();   // creates new object to return
//            status.RunId = proc.RunId;

//            Thread.Sleep(proc.Delay);

//            Process newProcess = null;
//            ProcessStartInfo execInfo = new ProcessStartInfo();
//            execInfo.FileName = proc.Cmd;
//            execInfo.Arguments = proc.Args;
//            execInfo.CreateNoWindow = false;
//            execInfo.UseShellExecute = false;
//            execInfo.RedirectStandardError = true;
//            if (proc.Hidden)
//                execInfo.WindowStyle = ProcessWindowStyle.Hidden;

//            try
//            {
//                newProcess = Process.Start(execInfo);

//                string stderr = newProcess.StandardError.ReadToEnd();       //get error output

//                newProcess.WaitForExit(proc.TimeOut);  // wait for process completion or timeout

//                status.ExitCode = newProcess.ExitCode;
//                if (status.ExitCode > 0)
//                    status.ErrorMessage = stderr;
//            }
//            catch (Exception)
//            {
//                status.ErrorMessage = "ERROR: Unable to start \"" + proc.Cmd + "\"";
//            }
//            finally
//            {
//                if (newProcess != null)
//                    newProcess.Close();
//            }

//            return status;
//        }
//    }
//}
