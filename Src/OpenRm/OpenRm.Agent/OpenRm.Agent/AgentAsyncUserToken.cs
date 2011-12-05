using System.Net.Sockets;
using OpenRm.Common.Entities.Network;

namespace OpenRm.Agent
{
    internal class AgentAsyncUserToken : AsyncUserTokenBase
    {
        public AgentAsyncUserToken() : base(null) {}

        public AgentAsyncUserToken(Socket socket, int msgPrefixLength = 4)
            : base(socket, msgPrefixLength)
        {
            
        }
    }
}
