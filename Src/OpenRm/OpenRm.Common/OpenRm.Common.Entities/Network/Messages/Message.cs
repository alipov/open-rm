namespace OpenRm.Common.Entities.Network.Messages
{

    public abstract class Message
    {
        public int OpCode;               // int type (not Enum) because of Serialization
    }

    public abstract class RequestBase
    {
    }

    public abstract class ResponseBase
    {
    }
}
