namespace OpenRm.Common.Entities.Network.Messages
{
    public class RunProcessResponse : ResponseBase
    {
        public int RunId;
        public int ExitCode = -1;
        public string ErrorMessage;
    }
}