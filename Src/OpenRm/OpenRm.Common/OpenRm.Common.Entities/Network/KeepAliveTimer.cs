using System.Timers;

namespace OpenRm.Common.Entities.Network
{
    class KeepAliveTimer : Timer
    {
        public AsyncUserTokenBase Token { get; set; }

        public KeepAliveTimer(AsyncUserTokenBase token) : base()
        {
            Token = token;
            Interval = 30000;  // 30 sec
            Enabled = true;
        }
    }
}


