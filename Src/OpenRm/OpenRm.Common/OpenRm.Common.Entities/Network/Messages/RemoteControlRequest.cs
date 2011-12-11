using System.Diagnostics;

namespace OpenRm.Common.Entities.Network.Messages
{
    public class RemoteControlRequest : RequestBase
    {
        public string ViewerIp;
        public int ViewerPort;

        public RemoteControlRequest() { }

        public RemoteControlRequest(string viewerIp, int viewerPort)
        {
            ViewerIp = viewerIp;
            ViewerPort = viewerPort;
        }


        public override ResponseBase ExecuteRequest()
        {
            string vncDir = "..\\Common\\ThirdParty\\VNC\\";
            string vncName = "OpenRM.winvnc.exe";
            bool running = true;
            var result = new RunProcessResponse();

            // Start VNC Server
            if (!IsProcessRunning(vncName))
            {
                var proc = new RunProcessRequest(
                    runId: 0,
                    cmd: vncDir + vncName,
                    args: "-run",
                    workDir: vncDir,
                    delay: 0,
                    hidden: true,
                    wait: false);

                result = (RunProcessResponse)proc.ExecuteRequest();
                if (result.ExitCode > 0)
                {
                    running = false;
                }
            }

            // connect to VNC listener
            if (running)
            {
                var proc = new RunProcessRequest(
                runId: 0,
                cmd: vncDir + vncName,
                args: "-connect " + ViewerIp + "::" + ViewerPort + " -shareall",
                workDir: vncDir,
                delay: 0,
                hidden: true,
                wait: false);

                result = (RunProcessResponse)proc.ExecuteRequest();
            }

            return new RemoteControlResponse(result.ExitCode, result.ErrorMessage);
        }


        // used by Console: starts VNC viewer on Console's computer
        public void StartVncListerner()
        {
            string vncViewName = "OpenRM.vncview.exe";

            if (!IsProcessRunning(vncViewName))
            {
                var proc = new RunProcessRequest(
                runId: 0,
                cmd: "..\\Common\\ThirdParty\\VNC\\" + vncViewName,
                args: "/listen " + ViewerPort,
                workDir: "..\\Common\\ThirdParty\\VNC\\",
                delay: 0,
                hidden: false,
                wait: false);

                proc.ExecuteRequest();    
            }

            
        }


        private bool IsProcessRunning(string processName)
        {
            Process[] processlist = Process.GetProcesses();

            foreach (Process process in processlist)
            {
                if ((process.ProcessName + ".exe").ToLower() == processName.ToLower())
                    return true;
            }
            return false;
        }

    }
}