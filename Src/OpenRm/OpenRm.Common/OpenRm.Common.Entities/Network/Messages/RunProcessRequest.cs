using System;
using System.Diagnostics;
using System.Threading;

namespace OpenRm.Common.Entities.Network.Messages
{
    public class RunProcessRequest : RequestBase
    {
        public int RunId;    // sequence number of execution (for response identification only)
        public string Cmd;      // path to program
        public string Args;  // arguments
        public string WorkDir;  //working directory
        public int Delay;       //delay period before run. in seconds.
        public int TimeOut;     //max time to wait for process execution completion. in ms.
        public bool Hidden;     //run hidden from user
        public RunProcessRequest(int runId, string cmd, string args, string workDir, int delay, int timeout, bool hidden)
        {
            RunId = runId;
            Cmd = cmd;
            Args = args;
            WorkDir = workDir;
            Delay = delay;
            TimeOut = timeout;
            Hidden = hidden;
        }

        public RunProcessRequest()
        {
            
        }

        public override ResponseBase ExecuteRequest()
        {
            RunProcessResponse status = new RunProcessResponse();   // creates new object to return
            status.RunId = RunId;

            Thread.Sleep(Delay);

            Process newProcess = null;
            ProcessStartInfo execInfo = new ProcessStartInfo();
            execInfo.FileName = Cmd;
            execInfo.Arguments = Args;
            execInfo.CreateNoWindow = false;
            execInfo.UseShellExecute = false;
            execInfo.RedirectStandardError = true;
            if (Hidden)
                execInfo.WindowStyle = ProcessWindowStyle.Hidden;

            try
            {
                newProcess = Process.Start(execInfo);

                string stderr = newProcess.StandardError.ReadToEnd();       //get error output

                newProcess.WaitForExit(TimeOut);  // wait for process completion or timeout

                status.ExitCode = newProcess.ExitCode;
                if (status.ExitCode > 0)
                    status.ErrorMessage = stderr;
            }
            catch (Exception)
            {
                status.ErrorMessage = "ERROR: Unable to start \"" + Cmd + "\"";
            }
            finally
            {
                if (newProcess != null)
                    newProcess.Close();
            }

            return status;
        }
    }
}