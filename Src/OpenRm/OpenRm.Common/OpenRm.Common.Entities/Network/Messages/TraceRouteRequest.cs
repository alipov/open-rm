namespace OpenRm.Common.Entities.Network.Messages
{
    public class TraceRouteRequest : RequestBase
    {
        public int RunId;       // sequence number for identification only
        public string Target;   //IP or DNS

        public TraceRouteRequest(){}
        public TraceRouteRequest(int runId, string target)
        {
            RunId = runId;
            Target = target;
        }
    }
}
