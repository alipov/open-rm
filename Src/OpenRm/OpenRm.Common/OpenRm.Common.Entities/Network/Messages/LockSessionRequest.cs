using System;
using System.Runtime.InteropServices;

namespace OpenRm.Common.Entities.Network.Messages
{
    public class LockSessionRequest : RequestBase
    {
        [DllImport("User32.dll")]
        internal static extern bool LockWorkStation();

        public override ResponseBase ExecuteRequest()
        {
            var lr = new LockSessionResponse();

            bool lockResult = false;
            try
            {
                lockResult = LockWorkStation();
                if (lockResult)
                    Logger.WriteStr(" Computer has been locked by OpenRM Agent.");
                else
                    Logger.WriteStr(" Failed to lock computer by OpenRM Agent due to error: " + Marshal.GetLastWin32Error());
            }
            catch (Exception ex)
            {
                Logger.WriteStr(" Failed to lock computer by OpenRM Agent due to error: " + ex.Message);
            }

            lr.Succeeded = lockResult;
            return lr;
        }
    }
}
