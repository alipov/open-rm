using System.Collections.Generic;
using OpenRm.Common.Entities.Executors;

namespace OpenRm.Common.Entities.Network.Messages
{
    public class PingRequest : RequestBase
    {
        public int RunId;       // sequence number for identification only
        public string Target;   //IP or DNS

        public PingRequest(){}
        public PingRequest(int runId, string target)
        {
            RunId = runId;
            Target = target;
        }

        public override ResponseBase ExecuteRequest()
        {
            //start with maximum ttl (32) in order to make just one ping
            var resultList = (List<string>)PingExecutor.SendPingWithTtl(Target, 32);
            var resultString = "";
            foreach (string str in resultList)
            {
                resultString += str + "\n";
            }
            return new RunCommonResponse(RunId, resultString);
        }
    }
}
