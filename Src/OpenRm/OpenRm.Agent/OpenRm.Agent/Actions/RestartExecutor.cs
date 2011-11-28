using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRm.Common.Entities;
using OpenRm.Common.Entities.Network.Messages;

namespace OpenRm.Agent.Actions
{
    public static class RestartExecutor
    {
        // Restart system
        public static ResponseBase Run(int runId)
        {
            var res = new RunCommonResponse(runId, "Shutdown process started");
            try
            {
                ShutdownExecutor.PerformShutdown("Reboot", true);
            }
            catch (Exception ex)
            {
                string err = "Error occured while trying to perform shutdown: " + ex.Message;
                Logger.WriteStr(err);
                res.Answer = err;
            }
            return res;
        }
    }
}
