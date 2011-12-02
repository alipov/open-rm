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
                timeout: 1000,         //wait 1sec just to enshure that it has not failed
                hidden: false);

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
                timeout: 1000,         //wait 1sec just to enshure that it has not failed
                hidden: false);

                result = (RunProcessResponse)proc.ExecuteRequest();
            }

            return new RemoteControlResponse(result.ExitCode, result.ErrorMessage);
        }
    }
}