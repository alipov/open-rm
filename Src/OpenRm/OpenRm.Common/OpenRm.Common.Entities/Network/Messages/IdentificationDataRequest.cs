using System;
using OpenRm.Common.Entities.Executors;

namespace OpenRm.Common.Entities.Network.Messages
{
    public class IdentificationDataRequest : RequestBase
    {
        public override ResponseBase ExecuteRequest()
        {
            var id = new IdentificationDataResponse
            {
                DeviceName = Environment.MachineName,
                SerialNumber = WmiQuery.GetWMIdata("Win32_BaseBoard", "SerialNumber")
            };
            return id;
        }
    }
}
