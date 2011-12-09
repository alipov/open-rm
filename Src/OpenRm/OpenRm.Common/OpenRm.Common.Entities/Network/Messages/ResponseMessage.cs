namespace OpenRm.Common.Entities.Network.Messages
{
    public class ResponseMessage : Message
    {
        public ResponseMessage(string uniqueId) 
            :base(uniqueId) { }

        public ResponseMessage() 
            :base(null) { }

        public ResponseBase Response;
        //public ResponseMessage()
        //{
        //    MessageType = (int)EMessageType.Response;
        //}
        //public int ResponseType;
    }
}