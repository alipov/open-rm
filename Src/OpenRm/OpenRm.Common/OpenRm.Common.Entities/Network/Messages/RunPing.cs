namespace OpenRm.Common.Entities.Network.Messages
{
    public class RunPing : RequestBase
    {
        public int RunId;       // sequence number for identification only
        public string Target;   //IP or DNS

        public RunPing(){}
        public RunPing(int runId, string target)
        {
            RunId = runId;
            Target = target;
        }
    }
}
