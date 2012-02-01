using System.Timers;

namespace OpenRm.Common.Entities.Network
{
    // Keep-Alive Timer lets to detect Half-Opened connections by periodic sending tiny packets to second connection side
    // TCP is an "idle" protocol, it assumes that the connection is active until proven otherwise. It designed to survive 
    //  on routers reboots, changes in routing paths, unplugged cables...
    // TCP uses ACKs to ensure that another side has recieved it's data. Thus, broken connections can be detected by sending 
    //  out data. So we'll use Timer to send empty messages each "Interval" of seconds.

    public class KeepAliveTimer : Timer
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


