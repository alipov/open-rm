namespace OpenRm.Common.Entities.Network.Messages
{
    public class WakeOnLanRequest : RequestBase
    {
        public string Mac;
        public int RunId;
        public string Ip;
        public string NetMask;

        public WakeOnLanRequest(){}
        public WakeOnLanRequest(string macAddress, string ip, string netMask, int runId)
        {
            Mac = macAddress;
            Ip = ip;
            NetMask = netMask;
            RunId = runId;
        }
    }
}
