namespace OpenRm.Common.Entities.Network.Messages
{
    public class RunProcess : RequestBase
    {
        public int RunId;    // sequence number of execution (for response identification only)
        public string Cmd;      // path to program
        public string Args;  // arguments
        public string WorkDir;  //working directory
        public int Delay;       //delay period before run. in seconds.
        public int TimeOut;     //max time to wait for process execution completion. in ms.
        public bool Hidden;     //run hidden from user
        public RunProcess(int runId, string cmd, string args, string workDir, int delay, int timeout, bool hidden)
        {
            RunId = runId;
            Cmd = cmd;
            Args = args;
            WorkDir = workDir;
            Delay = delay;
            TimeOut = timeout;
            Hidden = hidden;
        }

        public RunProcess()
        {
            
        }
    }
}