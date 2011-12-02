using OpenRm.Common.Entities.Network.Messages;

namespace OpenRm.Agent.Actions
{
    public class RemoteControlExecutor
    {
        public static ResponseBase Run(RemoteControlRequest remote)
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

            var result = (RunProcessResponse)ProcessExecutor.Run(proc);

            if (result.ExitCode <= 0)
            {
                // connect to VNC listener
                proc = new RunProcessRequest(
                runId: 0,
                cmd: "..\\Common\\ThirdParty\\VNC\\winvnc.exe",
                args: "-connect "+ remote.ViewerIp +"::"+ remote.ViewerPort +" -shareall",
                workDir: "..\\Common\\ThirdParty\\VNC\\",
                delay: 0,
                timeout: 1000,         //wait 1sec just to enshure that it has not failed
                hidden: false);

                result = (RunProcessResponse)ProcessExecutor.Run(proc);
            }

            return new RemoteControlResponse(result.ExitCode, result.ErrorMessage);
        }
    }
}
