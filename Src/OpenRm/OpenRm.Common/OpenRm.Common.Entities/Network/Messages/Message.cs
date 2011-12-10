namespace OpenRm.Common.Entities.Network.Messages
{
    public abstract class Message
    {
        protected Message(string uniqueId)
        {
            UniqueID = uniqueId;
        }

        public string UniqueID;
        public int AgentId;        //agent's identification (to match console-host-agent messages)
    }

    public abstract class RequestBase
    {
        public abstract ResponseBase ExecuteRequest();
    }

    public abstract class ResponseBase
    {
    }
}
