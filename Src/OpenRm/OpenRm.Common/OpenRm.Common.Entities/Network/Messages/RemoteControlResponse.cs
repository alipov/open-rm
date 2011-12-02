namespace OpenRm.Common.Entities.Network.Messages
{
    public class RemoteControlResponse : ResponseBase
    {
        public int ExitCode = -1;
        public string ErrorMessage;

        public RemoteControlResponse(){}
        public RemoteControlResponse(int exitCode, string errorMessage)
        {
            ExitCode = exitCode;
            ErrorMessage = errorMessage;
        }
    }
}
