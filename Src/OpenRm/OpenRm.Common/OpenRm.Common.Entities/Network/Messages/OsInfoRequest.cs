﻿using System;
using System.Collections.Generic;
using OpenRm.Common.Entities.Executors;

namespace OpenRm.Common.Entities.Network.Messages
{
    public class OsInfoRequest : RequestBase
    {
        public override ResponseBase ExecuteRequest()
        {
            OsInfoResponse os = new OsInfoResponse();

            //retrieve all data in one call because WMI call usually takes some time...
            string[] properties = new string[] { "Caption", "Version", "OSArchitecture", "TotalVisibleMemorySize", "FreePhysicalMemory", "SystemDrive" };
            Dictionary<string, string> values = WmiQuery.GetWMIdata("Win32_OperatingSystem", properties);
            os.OsName = values["Caption"];
            os.OsVersion = values["Version"];
            if (values.ContainsKey("OSArchitecture"))
                os.OsArchitecture = values["OSArchitecture"];
            else
                os.OsArchitecture = "32 bit";   //old 32-bit systems has no OSArchitecture property  
            os.RamSize = Int32.Parse(values["TotalVisibleMemorySize"])/1024;    //in Mb
            os.SystemDrive = values["SystemDrive"];
            os.SystemDriveSize = (int)(Double.Parse(WmiQuery.GetWMIdata("Win32_LogicalDisk", "Size", "Caption", os.SystemDrive))/(1024*1024*1024)); //Gb

            return os;
        }
    }
}
