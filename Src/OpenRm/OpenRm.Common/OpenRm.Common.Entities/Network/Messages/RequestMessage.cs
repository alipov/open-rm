namespace OpenRm.Common.Entities.Network.Messages
{
    public class RequestMessage : Message
    {
        public RequestMessage(string uniqueId) 
            :base(uniqueId) { }

        public RequestMessage() 
            :base(null) { }

        public RequestBase Request;

        //public int? AgentId;
        //public RequestMessage ()
        //{
        //    MessageType = (int)EMessageType.Request;
        //}
        //public int RequestType;
    }
}