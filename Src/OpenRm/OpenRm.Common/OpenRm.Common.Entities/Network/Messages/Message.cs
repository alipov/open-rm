namespace OpenRm.Common.Entities.Network.Messages
{
    public abstract class Message
    {
        protected Message(string uniqueId)
        {
            UniqueID = uniqueId;
        }

        public string UniqueID;
        public int OpCode;         // int type (not Enum) because of Serialization    //TODO: delete it
        public int AgentId;        //agent's identification (to match console-host-agent messages) //TODO: maybe change to Name...
    }

    public abstract class RequestBase
    {
        public abstract ResponseBase ExecuteRequest();
    }

    public abstract class ResponseBase
    {
    }
}
