using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OpenRm.Server.Gui.Inf.Api;
using OpenRm.Server.Gui.Modules.Monitor.Models;

namespace OpenRm.Server.Gui.Modules.Monitor.Api.Services
{
    public interface IAgentDataService : IService
    {
        IEnumerable<Agent> GetAgents(Predicate<Agent> predicate);
    }
}
