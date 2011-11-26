using System.Collections.Generic;

namespace OpenRm.Common.Entities.Network.Messages
{
    public class ListAgentsResponse : ResponseBase
    {
        public List<Agent> Agents { get; set; }
    }
}
