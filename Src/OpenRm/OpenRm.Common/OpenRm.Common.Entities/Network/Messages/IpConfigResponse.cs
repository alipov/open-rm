namespace OpenRm.Common.Entities.Network.Messages
{
    public class IpConfigResponse : ResponseBase
    {
        public string IpAddress { get; set; }
        public string NetMask { get; set; }
        public string DefaultGateway { get; set; }
        public string MAC { get; set; }
    }
}