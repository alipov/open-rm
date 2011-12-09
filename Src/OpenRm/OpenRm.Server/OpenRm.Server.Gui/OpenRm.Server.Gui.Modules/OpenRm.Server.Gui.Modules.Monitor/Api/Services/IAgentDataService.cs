using System;
using System.Collections.Generic;
using OpenRm.Server.Gui.Inf.Api;
using OpenRm.Server.Gui.Modules.Monitor.Models;

namespace OpenRm.Server.Gui.Modules.Monitor.Api.Services
{
    public interface IAgentDataService : IService
    {
        IEnumerable<AgentWrapper> GetAgents(Predicate<AgentWrapper> predicate);
        void SetAgents(IEnumerable<AgentWrapper> agents);
    }
}
