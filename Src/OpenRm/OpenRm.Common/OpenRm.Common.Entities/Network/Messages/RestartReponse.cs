namespace OpenRm.Common.Entities.Network.Messages
{
    public class RestartResponse : ResponseBase
    {
        public int RunId;    // sequence number for identification only
        public string Answer;

        public RestartResponse() {}
        public RestartResponse(int runId, string anwser)
        {
            RunId = runId;
            Answer = anwser;
        }

        public override string ToString()
        {
            return Answer;
        }
    }
}
