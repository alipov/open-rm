using System;
using System.Collections.Generic;
using OpenRm.Common.Entities.Network.Messages;

namespace OpenRm.Agent.Actions
{
    public static class PerfmonDataRetriever
    {
        public static ResponseBase Run( PerfmonDataRequest request)
        {
            var pf = new PerfmonDataResponse();
            string driveName = request.DriveName;

            pf.CPUuse = Int32.Parse(WmiQuery.GetWMIdata("Win32_PerfFormattedData_PerfOS_Processor", "PercentProcessorTime", "Name", "_Total"));
            pf.RAMfree = Int32.Parse(WmiQuery.GetWMIdata("Win32_PerfFormattedData_PerfOS_Memory", "AvailableMBytes"));
            string[] properties = new string[] { "FreeMegabytes", "AvgDiskQueueLength" };
            Dictionary<string, string> values = WmiQuery.GetWMIdata("Win32_PerfFormattedData_PerfDisk_LogicalDisk", properties, "Name", driveName);
            pf.DiskFree = Int32.Parse(values["FreeMegabytes"]);
            pf.DiskQueue = Int32.Parse(values["AvgDiskQueueLength"]);

            return pf;
        }

    }
}
