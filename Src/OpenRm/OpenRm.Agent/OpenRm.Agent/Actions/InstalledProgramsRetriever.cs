//using System.Collections.Generic;
//using System.Linq;
//using Microsoft.Win32;
//using OpenRm.Common.Entities.Network.Messages;

//namespace OpenRm.Agent.Actions
//{
//    public static class InstalledProgramsRetriever
//    {
//        public static ResponseBase Run()
//        {
//            var progs = new InstalledProgramsResponse();
//            var installedPrograms = new List<string>();

//            // look in two registry locations: 1st - for 32-bit application, 2nd - for 64-bit applications
//            var uninstallKey = new string[] { @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
//                                              @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall" };
//            foreach (string uninstKey in uninstallKey)
//            {
//                using (RegistryKey socketkey = Registry.LocalMachine.OpenSubKey(uninstKey))
//                {
//                    if (socketkey != null)
//                        foreach (string applKeyName in socketkey.GetSubKeyNames())
//                            using (RegistryKey applKey = socketkey.OpenSubKey(applKeyName))
//                            {
//                                if (applKey != null && applKey.GetValue("DisplayName") != null)
//                                    installedPrograms.Add(applKey.GetValue("DisplayName") + "  " + applKey.GetValue("DisplayVersion"));
//                            }
//                }
//            }

//            // Remove duplicates and sort in lexiographic order
//            List<string> result = installedPrograms.Distinct().ToList();
//            result.Sort();

//            progs.Progs = result;

//            return progs;
//        }
//    }
//}
