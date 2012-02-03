namespace OpenRm.Common.Entities.Network.Messages
{
    public class AgentStatusUpdate: ResponseBase
    {
        // Server sends status update to GUI on each agent status change.
        public int status { get; set; }

    }
}
