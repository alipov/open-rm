namespace OpenRm.Common.Entities.Network.Messages
{
    public class ShutdownResponse : ResponseBase
    {
        public int RunId;    // sequence number for identification only
        public string Answer;

        public ShutdownResponse() {}
        public ShutdownResponse(int runId, string anwser)
        {
            RunId = runId;
            Answer = anwser;
        }
    }
}
