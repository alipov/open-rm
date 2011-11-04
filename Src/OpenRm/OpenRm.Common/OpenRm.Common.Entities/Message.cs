namespace OpenRm.Common.Entities
{

    public abstract class Message
    {
        public EMessageType MessageType;
        public int OperationType;
        //public string 
    }

    public class RequestMessage : Message
    {
        public RequestBase Request;
        public RequestMessage ()
        {
            MessageType = EMessageType.Request;
        }
        //public int RequestType;
    }

    public class ResponseMessage : Message
    {
        public ResponseBase Response;
        public ResponseMessage()
        {
            MessageType = EMessageType.Response;
        }
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

    public class ConnectionEstablishmentRequest : RequestBase
    {
        public string Something;
    }
}
