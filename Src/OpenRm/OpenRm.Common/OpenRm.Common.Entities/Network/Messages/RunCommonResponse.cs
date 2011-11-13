namespace OpenRm.Common.Entities.Network.Messages
{
    public class RunCommonResponse
    {
        public int RunId;    // sequence number for identification only
        public string Answer;

        public RunCommonResponse() {}
        public RunCommonResponse(int runId, string anwser)
        {
            RunId = runId;
            Answer = anwser;
        }
    }
}
