using System;
using OpenRm.Common.Entities.Network.Messages;

namespace OpenRm.Agent.Actions
{
    public static class IdRetriever
    {
        public static ResponseBase Run()
        {
            var id = new IdentificationDataResponse
            {
                deviceName = Environment.MachineName,
                sn = WmiQuery.GetWMIdata("Win32_BaseBoard", "SerialNumber")
            };
            return id;
        }
    }
}
