using OpenRm.Common.Entities.Network.Messages;

namespace OpenRm.Common.Entities
{
    // This class holds base (commonly static) data of single client
    // Put here all info that should be stored in server's database
    public class ClientData
    {
        public IdentificationDataResponse Idata { get; set; }    // Name, Serial number
        public IpConfigResponse IpConfig { get; set; }       // IP, Netmask, MAC, Default Gateway
        public OsInfoResponse OS { get; set; }                   // OS Name, Version, Arcitecture (32/64 bit), RAM size, System Drive name, System Drive size
    }
}
