using System.Net.Sockets;
using OpenRm.Common.Entities;
using OpenRm.Common.Entities.Network;

namespace OpenRm.Server.Host.Network
{
    internal class HostAsyncUserToken : AsyncUserTokenBase
    {
        public HostAsyncUserToken() : base(null, null) {}

        public HostAsyncUserToken(Socket socket, ClientData data, int msgPrefixLength = 4)
            : base(socket, data, msgPrefixLength)
        {
            
        }

        public static int runId = 1;

        // returns and increments process identification number
        public int GetRunId()
        {
            return runId++;
        }
    }
}
