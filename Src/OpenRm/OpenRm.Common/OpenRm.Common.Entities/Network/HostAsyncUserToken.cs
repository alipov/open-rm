using System;

namespace OpenRm.Common.Entities.Network
{
    public class HostAsyncUserToken : AsyncUserTokenBase
    {
        public HostAsyncUserToken() : base(null) { }

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

        public Agent Agent { get; set; }

        public Action<HostCustomEventArgs> Callback { get; set; }
    }
}
