
using System;
using System.Collections.Generic;

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
            os.RamSize = Int32.Parse(values["TotalVisibleMemorySize"]);
            os.SystemDrive = values["SystemDrive"];
            os.SystemDriveSize = Int32.Parse(WmiQuery.GetWMIdata("Win32_LogicalDisk", os.SystemDrive));

            return os;
        }
    }
}
