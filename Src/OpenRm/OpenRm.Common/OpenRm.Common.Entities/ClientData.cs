using System;
using OpenRm.Common.Entities.Network.Messages;

namespace OpenRm.Common.Entities
{
    // This class holds all data of single client
    public class ClientData
    {
        //TODO: add more objects:
        public IdentificationData Idata { get; set; }    // Name, Serial number
        public IpConfigData IpConfig { get; set; }       // IP, Netmask, MAC, Default Gateway
        public OsInfo OS { get; set; }                   // OS Name, Version, Arcitecture (32/64 bit), RAM size, System Drive name, System Drive size
        public InstalledPrograms apps { get; set; }      // Add-Remove Programs info
        // ...


    }
}
