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

        static HostAsyncUserToken()
        {
            _runId = 1;
        }

        private static int _runId;
        public static int RunId
        {
            get // returns and increments process identification number
            {
                return _runId++;
            }
        }
    }
}
