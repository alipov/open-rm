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

    public class OsInfo : ResponseBase
    {
        public string OsName;
        public string OsVersion;
        public string OsArchitecture;
        public int CdriveSize;
        public int CdriveFreeSpace;
        public int RamSize;
        public int FreeRamSize;

    }

    public class RunProcess : RequestBase
    {
        public int RunId;    // sequence number of execution (for response identification only)
        public string Cmd;      // path to program
        public string Args;  // arguments
        public string WorkDir;  //working directory
        public int Delay;       //delay period before run. in seconds.
        public int TimeOut;     //max time to wait for process execution completion. in ms.
        public bool Hidden;     //run hidden from user
        public RunProcess(int runId, string cmd, string args, string workDir, int delay, int timeout, bool hidden)
        {
            RunId = runId;
            Cmd = cmd;
            Args = args;
            WorkDir = workDir;
            Delay = delay;
            TimeOut = timeout;
            Hidden = hidden;
        }

        public RunProcess()
        {
            
        }
    }

    public class RunCompletedStatus : ResponseBase
    {
        public int RunId;
        public int ExitCode = -1;
        public string ErrorMessage;
    }


}
