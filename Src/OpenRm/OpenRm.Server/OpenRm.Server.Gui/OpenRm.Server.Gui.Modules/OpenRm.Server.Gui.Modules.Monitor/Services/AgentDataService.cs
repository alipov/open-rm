using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Practices.Prism.Events;
using OpenRm.Common.Entities;
using OpenRm.Server.Gui.Modules.Monitor.Api.Services;
using OpenRm.Server.Gui.Modules.Monitor.EventAggregatorMessages;

namespace OpenRm.Server.Gui.Modules.Monitor.Services
{
    public class AgentDataService : IAgentDataService
    {
        private readonly List<Agent> _agentsCollection;
        private readonly IEventAggregator _eventAggregator;

        public AgentDataService(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
            _agentsCollection = new List<Agent>();
            //_agentsCollection = new List<Agent>()
            //                        {
            //                            new Agent()
            //                                {
            //                                    ID = 1,
            //                                    Name = "First agent"
            //                                },
            //                            new Agent()
            //                                {
            //                                    ID = 2,
            //                                    Name = "Second agent"
            //                                }
            //                        };
        }

        public IEnumerable<Agent> GetAgents(Predicate<Agent> predicate)
        {
            return _agentsCollection.Where(agent => predicate(agent));
        }

        public void SetAgents(IEnumerable<Agent> agents)
        {
            _agentsCollection.Clear();
            _agentsCollection.AddRange(agents);
            _eventAggregator.GetEvent<AgentsListUpdated>().Publish(1);
        }
    }
}
