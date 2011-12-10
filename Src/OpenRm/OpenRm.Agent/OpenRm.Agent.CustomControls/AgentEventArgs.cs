using System;
using System.Net;

namespace OpenRm.Agent.CustomControls
{
    // used in transferring parameters between Agent's core and it's GUI
    public class AgentEventArgs : EventArgs
    {
        public IPEndPoint ServerEP { get; private set; }

        public AgentEventArgs(IPEndPoint serverEP)
        {
            ServerEP = serverEP;
        }
    }
}
