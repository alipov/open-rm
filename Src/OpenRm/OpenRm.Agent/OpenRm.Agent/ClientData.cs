using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using OpenRm.Common.Entities;


namespace OpenRm.Agent
{
    // This class holds all data of single client
    class ClientData
    {
//TODO: replace all int/strings/IPaddresses with Common.Entities objects:
        public IdentificationData Idata { get; set; }    // Computer name is it's identifier
        public IpConfigData IpConfig { get; set; } 
        public String OStype { get; set; }
        public String CPU { get; set; }
        public Int32 RAM { get; set; }
        // ...


    }

}
