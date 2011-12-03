using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRm.Common.Entities.Enums;

namespace OpenRm.Common.Entities
{
    public class Agent
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public EAgentStatus Status { get; set; }

        public ClientData Data { get; set; }
    }
}
