//using System;
//using System.Runtime.InteropServices;
//using OpenRm.Common.Entities;
//using OpenRm.Common.Entities.Network.Messages;

//namespace OpenRm.Agent.Actions
//{
//    public static class LockSessionExecutor
//    {
//        [DllImport("User32.dll")]
//        internal static extern bool LockWorkStation();

//        public static ResponseBase Run()
//        {
//            var lr = new LockSessionResponse();

//            bool lockResult = false;
//            try
//            {
//                lockResult = LockWorkStation();
//                if (lockResult)
//                    Logger.WriteStr(" Computer has been locked by OpenRM Agent.");
//                else
//                    Logger.WriteStr(" Failed to lock computer by OpenRM Agent due to error: " + Marshal.GetLastWin32Error());
//            }
//            catch (Exception ex)
//            {
//                Logger.WriteStr(" Failed to lock computer by OpenRM Agent due to error: " + ex.Message);
//            }

//            lr.Succeeded = lockResult;
//            return lr;
//        }
//    }
//}
