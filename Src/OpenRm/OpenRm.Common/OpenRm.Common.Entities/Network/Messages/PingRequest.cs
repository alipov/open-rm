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
    }
}
