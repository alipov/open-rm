namespace OpenRm.Common.Entities.Network.Messages
{
    public class RunCompletedStatus : ResponseBase
    {
        public int RunId;
        public int ExitCode = -1;
        public string ErrorMessage;
    }
}