using System;

namespace OpenRm.Common.Entities.Network
{
    public class HostAsyncUserToken : AsyncUserTokenBase
    {
        public HostAsyncUserToken() : base(null, null) { }

        //public HostAsyncUserToken(Socket socket, ClientData data, int msgPrefixLength = 4)
        //    : base(socket, data, msgPrefixLength)
        //{

        //}

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

        //public IdentificationDataResponse ClientData { get; set; }
        public Agent Agent { get; set; }
        //public ClientData AgentInventory { get; set; }
        public Action<HostCustomEventArgs> Callback { get; set; }
    }
}
