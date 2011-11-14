namespace OpenRm.Common.Entities.Network.Messages
{
    class SendWakeOnLan : RequestBase
    {
        private string mac;

        public SendWakeOnLan(){}
        public SendWakeOnLan(string macAddress)
        {
            mac = macAddress;
        }
    }
}
