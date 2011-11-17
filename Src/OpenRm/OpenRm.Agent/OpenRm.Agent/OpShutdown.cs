using System;
using System.ComponentModel;
using System.Runtime.InteropServices;


namespace OpenRm.Agent
{
    public static class OpShutdown
    {
        const int PrivilegeEnabled = 0x00000002;
        const int TokenQuery = 0x00000008;
        const int AdjustPrivileges = 0x00000020;
        const string ShutdownPrivilege = "SeShutdownPrivilege";

        const uint SHTDN_REASON_MAJOR_OTHER = 0x00000000;
        const uint SHTDN_REASON_MINOR_MAINTENANCE = 0x00000001;
        const uint SHTDN_REASON_FLAG_PLANNED = 0x80000000;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct TokenPrivileges
        {
            public int PrivilegeCount;
            public long Luid;
            public int Attributes;
        }

        [DllImport("kernel32.dll")]
        internal static extern IntPtr GetCurrentProcess();

        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern int OpenProcessToken(IntPtr processHandle, int desiredAccess, ref IntPtr tokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern int LookupPrivilegeValue(string systemName, string name, ref long luid);

        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern int AdjustTokenPrivileges(IntPtr tokenHandle, bool disableAllPrivileges, ref TokenPrivileges newState, int bufferLength, IntPtr previousState, IntPtr length);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool InitiateSystemShutdownEx(
            string lpMachineName,
            string lpMessage,
            uint dwTimeout,
            bool bForceAppsClosed,
            bool bRebootAfterShutdown,
            uint dwReason);

        
        // Shutdown system
        public static void Shutdown()
        {
            PerformShutdown("Shutdown", false);
        }

        // Restart system
        public static void Reboot()
        {
            PerformShutdown("Reboot", true);
        }


        private static void PerformShutdown(string lpMessage, bool bRebootAfterShutdown)
        {
            ElevatePrivileges();

            if (!InitiateSystemShutdownEx(null, lpMessage + " has been initiatedby OpenRM Agent", 0, true, bRebootAfterShutdown,
                                     SHTDN_REASON_MAJOR_OTHER | SHTDN_REASON_MINOR_MAINTENANCE | SHTDN_REASON_FLAG_PLANNED))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());    
            }
        }


        private static void ElevatePrivileges()
        {
            IntPtr currentProcess = GetCurrentProcess();
            IntPtr tokenHandle = IntPtr.Zero;

            int result = OpenProcessToken(currentProcess, AdjustPrivileges | TokenQuery, ref tokenHandle);
            if (result == 0) throw new Win32Exception(Marshal.GetLastWin32Error());

            TokenPrivileges tokenPrivileges;
            tokenPrivileges.PrivilegeCount = 1;
            tokenPrivileges.Luid = 0;
            tokenPrivileges.Attributes = PrivilegeEnabled;

            result = LookupPrivilegeValue(null, ShutdownPrivilege, ref tokenPrivileges.Luid);
            if (result == 0) throw new Win32Exception(Marshal.GetLastWin32Error());

            result = AdjustTokenPrivileges(tokenHandle, false, ref tokenPrivileges, 0, IntPtr.Zero, IntPtr.Zero);
            if (result == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
        }


    }
}
