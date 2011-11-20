using System;
using System.Collections.Generic;
using OpenRm.Common.Entities;
using OpenRm.Server.Gui.Inf.Api;

namespace OpenRm.Server.Gui.Modules.Monitor.Api.Services
{
    public interface IAgentDataService : IService
    {
        IEnumerable<Agent> GetAgents(Predicate<Agent> predicate);
        void SetAgents(IEnumerable<Agent> agents);
    }
}
