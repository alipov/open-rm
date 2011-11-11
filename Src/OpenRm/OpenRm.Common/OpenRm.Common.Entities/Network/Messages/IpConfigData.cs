namespace OpenRm.Common.Entities.Network.Messages
{
    public class IpConfigData : ResponseBase
    {
        public string IpAddress;
        public string netMask;
        public string defaultGateway;
    }
}