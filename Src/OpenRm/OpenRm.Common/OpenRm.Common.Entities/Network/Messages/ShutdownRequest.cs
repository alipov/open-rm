
using System;
using OpenRm.Common.Entities.Executors;

namespace OpenRm.Common.Entities.Network.Messages
{
    public class ShutdownRequest : RequestBase
    {
        public int RunId { get; set; }

        public override ResponseBase ExecuteRequest()
        {
            var res = new ShutdownResponse(RunId, "Shutdown process started");
            try
            {
                ShutdownExecutor.PerformShutdown("Shutdown", false);
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
