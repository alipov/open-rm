using System;
using OpenRm.Common.Entities.Executors;

namespace OpenRm.Common.Entities.Network.Messages
{
    public class PerfmonDataRequest : RequestBase
    {
        private static int? realDiskFreeValue;
        private static int? realDiskQueueValue;
        public override ResponseBase ExecuteRequest()
        {
            var pf = new PerfmonDataResponse();

            pf.CPUuse = Int32.Parse(WmiQuery.GetWMIdata("Win32_PerfFormattedData_PerfOS_Processor", "PercentProcessorTime", "Name", "_Total"));
            pf.RAMfree = Int32.Parse(WmiQuery.GetWMIdata("Win32_PerfFormattedData_PerfOS_Memory", "AvailableMBytes"));
            string[] properties = new string[] { "FreeMegabytes", "AvgDiskQueueLength" };
            var values = WmiQuery.GetWMIdata("Win32_PerfFormattedData_PerfDisk_LogicalDisk", properties, "Name", "C:");

            pf.DiskFree = Int32.Parse(values["FreeMegabytes"]);
            pf.DiskQueue = Int32.Parse(values["AvgDiskQueueLength"]);

            #region Workaround for showing the graph in UI
            if (!realDiskFreeValue.HasValue)
            {
                pf.DiskFree -= 1;
                realDiskFreeValue = pf.DiskFree;
            }
            if (!realDiskQueueValue.HasValue)
            {
                pf.DiskQueue += 1;
                realDiskQueueValue = pf.DiskQueue;
            }
            #endregion

            return pf;
        }
    }
}
