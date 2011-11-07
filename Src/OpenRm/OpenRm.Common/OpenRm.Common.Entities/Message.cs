namespace OpenRm.Common.Entities
{

    public abstract class Message
    {
        //public int MessageType;         //TODO: Why we need it???
        public int OpCode;               // int type (not Enum) because of Serialization
    }

    public class RequestMessage : Message
    {
        public RequestBase Request;
        //public RequestMessage ()
        //{
        //    MessageType = (int)EMessageType.Request;
        //}
        //public int RequestType;
    }

    public class ResponseMessage : Message
    {
        public ResponseBase Response;
        //public ResponseMessage()
        //{
        //    MessageType = (int)EMessageType.Response;
        //}
        //public int ResponseType;
    }

    public abstract class RequestBase
    {
        //public string Info;
    }

    public abstract class ResponseBase
    {
        
    }

    
    public class IdentificationData : ResponseBase
    {
        public string deviceName;       // computer name for Windows OS, or IMEI for mobile devices
        public string sn;               // serial number (for Windows OS only)
    }

    public class IpConfigData : ResponseBase
    {
        public string IpAddress;
        public string netMask;
        public string defaultGateway;
    }


    public class RunProcess : RequestBase
    {
        public int RunId;    // sequence number of execution (for response identification only)
        public string Cmd;      // path to program
        public string Args;  // arguments
        public int Priority;    // Process priority
        public int TimeOut;     //time-out period before run. in seconds.
        public RunProcess(int runId, string cmd, string args, int priority, int timeOut)
        {
            RunId = runId;
            Cmd = cmd;
            Args = args;
            Priority = priority;
            TimeOut = timeOut;
        }
    }

    public class RunCompletedStatus : ResponseBase
    {
        public int RunId;
        public int ExitCode;
        public string ErrorMessage;
    }


}
