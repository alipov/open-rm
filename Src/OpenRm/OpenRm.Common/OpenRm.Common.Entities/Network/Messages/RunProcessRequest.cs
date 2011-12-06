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
        public bool Hidden;     //run hidden from user
        public bool WaitForCompletion;      // wait for process to complete, or just run and return
        public const int Timeout = 1800000;     // limit process run for 30 min

        public RunProcessRequest(int runId, string cmd, string args, string workDir, int delay, bool hidden, bool wait)
        {
            RunId = runId;
            Cmd = cmd;
            Args = args;
            WorkDir = workDir;
            Delay = delay;
            Hidden = hidden;
            WaitForCompletion = wait;
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
                Logger.WriteStr(" Executing process: \"" + Cmd + "\"...");
                newProcess = Process.Start(execInfo);

                if (WaitForCompletion)
                {
                    // wait for process completion
                    newProcess.WaitForExit(Timeout);   //wait maximum 30 min.   
                }
                else
                {
                    //Wait for window to finish loading.
                    newProcess.WaitForInputIdle();

                }

                if (!newProcess.HasExited)
                {
                    if (newProcess.Responding)
                    {
                        if (WaitForCompletion)
                        {
                            status.ErrorMessage = " Process is still running, but we have reached timeout (30min)";    
                        }
                        else
                        {
                            status.ErrorMessage = " Process sucessfully started and will take some time to run...";
                        }
                        
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
                    {
                            status.ErrorMessage = " Process completed with error. ";  // not all processes have stderr
                    }
                        
                }
            }
            catch (Exception)
            {
                status.ErrorMessage = "ERROR: Unable to start \"" + Cmd + " " + Args + "\"";
            }
            finally
            {
                if (newProcess != null)
                    newProcess.Close();

                Logger.WriteStr(" \"" + Cmd + "\":" + status.ErrorMessage +".");
            }

            return status;
        }
    }
}