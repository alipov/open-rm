
using System;
using System.Collections.Generic;

namespace OpenRm.Common.Entities.Network.Messages
{
    public class PerfmonDataRequest : RequestBase
    {
        //public string DriveName { get; set; }

        public override ResponseBase ExecuteRequest()
        {
            var pf = new PerfmonDataResponse();

            pf.CPUuse = Int32.Parse(WmiQuery.GetWMIdata("Win32_PerfFormattedData_PerfOS_Processor", "PercentProcessorTime", "Name", "_Total"));
            pf.RAMfree = Int32.Parse(WmiQuery.GetWMIdata("Win32_PerfFormattedData_PerfOS_Memory", "AvailableMBytes"));
            string[] properties = new string[] { "FreeMegabytes", "AvgDiskQueueLength" };
            var values = WmiQuery.GetWMIdata("Win32_PerfFormattedData_PerfDisk_LogicalDisk", properties, "Name", "C:");

            pf.DiskFree = Int32.Parse(values["FreeMegabytes"]);
            pf.DiskQueue = Int32.Parse(values["AvgDiskQueueLength"]);

            return pf;
        }
    }
}
