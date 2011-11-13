namespace OpenRm.Common.Entities.Network.Messages
{
    public class RunTraceRoute : RequestBase
    {
        public int RunId;       // sequence number for identification only
        public string Target;   //IP or DNS

        public RunTraceRoute(){}
        public RunTraceRoute(int runId, string target)
        {
            RunId = runId;
            Target = target;
        }
    }
}
