namespace OpenRm.Common.Entities.Network.Messages
{
    public class WakeOnLanRequest : RequestBase
    {
        public string Mac;
        public int RunId;

        public WakeOnLanRequest(){}
        public WakeOnLanRequest(int runId, string macAddress)
        {
            Mac = macAddress;
            RunId = runId;
        }
    }
}
