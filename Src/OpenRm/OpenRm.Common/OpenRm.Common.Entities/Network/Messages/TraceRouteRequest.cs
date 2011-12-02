using System.Collections.Generic;
using OpenRm.Common.Entities.Executors;

namespace OpenRm.Common.Entities.Network.Messages
{
    public class TraceRouteRequest : RequestBase
    {
        // sequence number for identification only
        public int RunId { get; set; }

        //IP or DNS
        public string Target { get; set; }

        public TraceRouteRequest(){}
        public TraceRouteRequest(int runId, string target)
        {
            RunId = runId;
            Target = target;
        }

        public override ResponseBase ExecuteRequest()
        {
            var resultList = (List<string>)PingExecutor.SendPingWithTtl(Target, 1);     //start with minimum ttl in order to increase it recursively
            var resultString = "";
            foreach (string str in resultList)
            {
                resultString += str + "\n";
            }

            return new RunCommonResponse(RunId, resultString);
        }
    }
}
