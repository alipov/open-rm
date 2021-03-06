using System;
using System.Diagnostics;
using Microsoft.Win32;

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
            var result = new RunProcessResponse();

            // Start VNC Server
            if (!IsProcessRunning(vncName))
            {
                //add registry settings to let silent application start
                byte[] pass = {1, 2, 3, 4, 5, 6, 7, 8};
                try
                {
                    Registry.SetValue("HKEY_CURRENT_USER\\Software\\ORL\\WinVNC3", "Password", pass,
                                      RegistryValueKind.Binary);

                    var proc = new RunProcessRequest(
                        runId: 0,
                        cmd: vncDir + vncName,
                        args: "-run",
                        workDir: vncDir,
                        delay: 0,
                        hidden: true,
                        wait: false);

                    result = (RunProcessResponse) proc.ExecuteRequest();
                }
                catch (Exception ex)
                {
                    Logger.WriteStr(" Cannot start VNC server due to error: " + ex.Message);
                }
            }

            // connect to VNC listener
            if (IsProcessRunning(vncName))
            {
                string arguments = "-connect " + ViewerIp + "::" + ViewerPort + " -shareall";
                Logger.WriteStr("Going to launch VNC server with parameters: " + arguments);

                var proc = new RunProcessRequest(
                runId: 0,
                cmd: vncDir + vncName,
                args: arguments,
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