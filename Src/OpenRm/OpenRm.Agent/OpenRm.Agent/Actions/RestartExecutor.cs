using System;
using OpenRm.Common.Entities;
using OpenRm.Common.Entities.Network.Messages;

namespace OpenRm.Agent.Actions
{
    public static class RestartExecutor
    {
        // Restart system
        public static ResponseBase Run(RestartRequest request)
        {
            var res = new RunCommonResponse(request.RunId, "Shutdown process started");
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
