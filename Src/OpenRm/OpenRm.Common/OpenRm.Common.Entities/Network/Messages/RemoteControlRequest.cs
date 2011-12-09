namespace OpenRm.Common.Entities.Network.Messages
{
    public class RemoteControlRequest : RequestBase
    {
        public string ViewerIp;
        public int ViewerPort;

        public  RemoteControlRequest() { }

        public RemoteControlRequest(string viewerIp, int viewerPort)
        {
            ViewerIp = viewerIp;
            ViewerPort = viewerPort;
        }


        public override ResponseBase ExecuteRequest()
        {
            // Start VNC Server
            RunProcessRequest proc = new RunProcessRequest(
                runId: 0,
                cmd: "..\\Common\\ThirdParty\\VNC\\winvnc.exe",
                args: "-run",
                workDir: "..\\Common\\ThirdParty\\VNC\\",
                delay: 0,
                hidden: true,
                wait: false);

            var result = (RunProcessResponse) proc.ExecuteRequest();

            if (result.ExitCode <= 0)
            {
                // connect to VNC listener
                proc = new RunProcessRequest(
                runId: 0,
                cmd: "..\\Common\\ThirdParty\\VNC\\winvnc.exe",
                args: "-connect " + ViewerIp + "::" + ViewerPort + " -shareall",
                workDir: "..\\Common\\ThirdParty\\VNC\\",
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
            var proc = new RunProcessRequest(
                runId: 0,
                cmd: "..\\Common\\ThirdParty\\VNC\\vncview.exe",
                args: "/listen " + ViewerPort,
                workDir: "..\\Common\\ThirdParty\\VNC\\",
                delay: 0,
                hidden: false,
                wait: false);

            proc.ExecuteRequest();
        }

    }
}