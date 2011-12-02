using System;
using OpenRm.Common.Entities.Network.Messages;

namespace OpenRm.Common.Entities.Network
{
    public class HostAsyncUserToken : AsyncUserTokenBase
    {
        public HostAsyncUserToken() : base(null, null) { }

        //public HostAsyncUserToken(Socket socket, ClientData data, int msgPrefixLength = 4)
        //    : base(socket, data, msgPrefixLength)
        //{

        //}

        private static int _runId;
        //public IdentificationDataResponse ClientData { get; set; }
        public ClientData AgentInventory { get; set; }

        static HostAsyncUserToken()
        {
            _runId = 1;

        }

        
        public static int RunId
        {
            get // returns and increments process identification number
            {
                return _runId++;
            }
        }

        public Action<HostCustomEventArgs> Callback { get; set; }
    }
}
