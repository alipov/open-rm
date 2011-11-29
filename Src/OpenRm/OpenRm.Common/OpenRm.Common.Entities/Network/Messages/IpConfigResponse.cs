namespace OpenRm.Common.Entities.Network.Messages
{
    public class IpConfigResponse : ResponseBase
    {
        public string IpAddress;
        public string netMask;
        public string defaultGateway;
        public string mac;
    }
}