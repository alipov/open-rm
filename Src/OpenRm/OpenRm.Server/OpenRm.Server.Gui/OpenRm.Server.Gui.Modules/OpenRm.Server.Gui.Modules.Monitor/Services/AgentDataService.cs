using System;
using System.Collections.Generic;
using System.Linq;
using OpenRm.Common.Entities;
using OpenRm.Server.Gui.Modules.Monitor.Api.Services;

namespace OpenRm.Server.Gui.Modules.Monitor.Services
{
    public class AgentDataService : IAgentDataService
    {
        private readonly List<Agent> _agentsCollection;

        public AgentDataService()
        {
            _agentsCollection = new List<Agent>()
                                    {
                                        new Agent()
                                            {
                                                ID = 1,
                                                Name = "First agent"
                                            },
                                        new Agent()
                                            {
                                                ID = 2,
                                                Name = "Second agent"
                                            }
                                    };
        }

        public IEnumerable<Agent> GetAgents(Predicate<Agent> predicate)
        {
            return _agentsCollection.Where(agent => predicate(agent));
        }
    }
}
