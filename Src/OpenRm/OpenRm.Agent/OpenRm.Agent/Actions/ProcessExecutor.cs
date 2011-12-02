using System;
using System.Diagnostics;
using System.Threading;
using OpenRm.Common.Entities.Network.Messages;

namespace OpenRm.Agent.Actions
{
    public static class ProcessExecutor
    {
        // Runs executable provided by "proc" parameter, which also holds information how this process should be executed
        public static ResponseBase Run(RunProcessRequest proc)
        {
            RunProcessResponse status = new RunProcessResponse();   // creates new object to return
            status.RunId = proc.RunId;

            Thread.Sleep(proc.Delay);

            Process newProcess = null;
            ProcessStartInfo execInfo = new ProcessStartInfo();
            execInfo.FileName = proc.Cmd;
            execInfo.Arguments = proc.Args;
            execInfo.CreateNoWindow = false;
            execInfo.UseShellExecute = false;
            execInfo.RedirectStandardError = true;
            if (proc.Hidden)
                execInfo.WindowStyle = ProcessWindowStyle.Hidden;

            try
            {
                newProcess = Process.Start(execInfo);

                string stderr = newProcess.StandardError.ReadToEnd();       //get error output

                newProcess.WaitForExit(proc.TimeOut);  // wait for process completion or timeout
                if (!newProcess.HasExited)
                {
                    if (newProcess.Responding)
                    {
                        status.ErrorMessage = " Process is still running, but we have reached timeout (" + proc.TimeOut + "ms)";    
                    }
                    else
                    {
                        // not responding so kill it
                        newProcess.Kill();
                        status.ExitCode = newProcess.ExitCode;
                        status.ErrorMessage = " Process was not responding. We've terminated it.";
                    }
                    
                }
                else
                {
                    status.ExitCode = newProcess.ExitCode;
                    if (status.ExitCode > 0)
                        status.ErrorMessage = stderr;    // not all processes have stderr
                }
            }
            catch (Exception)
            {
                status.ErrorMessage = "ERROR: Unable to start \"" + proc.Cmd + " " + proc.Args + "\"";
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
