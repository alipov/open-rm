using System;
using OpenRm.Common.Entities.Network.Messages;

namespace OpenRm.Common.Entities
{
    // This class holds all data of single client
    public class ClientData
    {
        //TODO: replace all int/strings/IPaddresses with Common.Entities objects:
        public IdentificationData Idata { get; set; }    // Computer name is it's identifier
        public IpConfigData IpConfig { get; set; }
        public OsInfo OS { get; set; }
        // ...


    }
}
