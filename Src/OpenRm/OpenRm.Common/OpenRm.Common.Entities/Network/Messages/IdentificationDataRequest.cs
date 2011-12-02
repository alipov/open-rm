using System;

namespace OpenRm.Common.Entities.Network.Messages
{
    public class IdentificationDataRequest : RequestBase
    {
        public override ResponseBase ExecuteRequest()
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
