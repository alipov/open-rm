using System.Net.Sockets;
using OpenRm.Common.Entities;
using OpenRm.Common.Entities.Network;

namespace OpenRm.Agent
{
    internal class AgentAsyncUserToken : AsyncUserTokenBase
    {
        public AgentAsyncUserToken() : base(null, null) {}

        public AgentAsyncUserToken(Socket socket, ClientData data, int msgPrefixLength = 4)
            : base(socket, data, msgPrefixLength)
        {
            
        }
    }
}
