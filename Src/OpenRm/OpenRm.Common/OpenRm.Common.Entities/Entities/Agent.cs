using OpenRm.Common.Entities.Enums;

namespace OpenRm.Common.Entities
{
    public class Agent
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int Status { get; set; }

        public ClientData Data { get; set; }
    }
}
