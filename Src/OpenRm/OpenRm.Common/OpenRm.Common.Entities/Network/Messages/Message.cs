namespace OpenRm.Common.Entities.Network.Messages
{

    public abstract class Message
    {
        //public int MessageType;         //TODO: Why we need it???
        public int OpCode;               // int type (not Enum) because of Serialization
    }

    public abstract class RequestBase
    {
        //public string Info;
    }

    public abstract class ResponseBase
    {
        
    }
}
