using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using OpenRm.Common.Entities;
using System.Management;
using System.Net.NetworkInformation;
using OpenRm.Common.Entities.Network.Messages;

namespace OpenRm.Agent
{
    public static class OpProcessor
    {

        public static void GetInfo(IdentificationData id)
        {
            id.deviceName = System.Environment.MachineName;
            id.sn = GetWMIdata("Win32_BaseBoard", "SerialNumber");
        }


        public static void GetInfo(IpConfigData ipconf, string sourceIP)
        {    
            ipconf.IpAddress = sourceIP;        // IP got from socket info

            foreach (NetworkInterface netInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (netInterface.OperationalStatus == OperationalStatus.Up)
                {
                    foreach (GatewayIPAddressInformation g in netInterface.GetIPProperties().GatewayAddresses)
                    {
                        if (g.Address.ToString() != "0.0.0.0")
                            ipconf.defaultGateway = g.Address.ToString();
                    }

                    foreach( UnicastIPAddressInformation ip in netInterface.GetIPProperties().UnicastAddresses )
                        if (ip.Address.ToString() == sourceIP)
                        {
                            if (ip.IPv4Mask != null)
                                ipconf.netMask = ip.IPv4Mask.ToString();
                            else
                                ipconf.netMask = "";
                            break;
                        }
                }
            }
        }

        public static void GetInfo(OsInfo os)
        {
            //retrieve all data in one call because WMI call usually takes some time...
            string[] properties = new string[] { "Caption", "Version", "OSArchitecture", "TotalVisibleMemorySize", "FreePhysicalMemory" };
            Dictionary<string, string> values = GetWMIdata("Win32_OperatingSystem", properties);    
            os.OsName = values["Caption"];
            os.OsVersion = values["Version"];
            if (values.ContainsKey("OSArchitecture"))
                os.OsArchitecture = values["OSArchitecture"];
            else
                os.OsArchitecture = "32 bit";   //old 32-bit systems has no OSArchitecture property  
            os.RamSize = Int32.Parse(values["TotalVisibleMemorySize"]);
            os.SystemDrive = values["SystemDrive"];
            os.SystemDriveSize = Int32.Parse(GetWMIdata("Win32_LogicalDisk", os.SystemDrive));
        }


        public static void GetInfo(PerfmonData pf, string diskName)
        {
            pf.CPUuse = Int32.Parse(GetWMIdata("Win32_PerfFormattedData_PerfOS_Processor", "PercentProcessorTime", "Name", "_Total"));
            pf.RAMfree = Int32.Parse(GetWMIdata("Win32_PerfFormattedData_PerfOS_Memory", "AvailableMBytes"));
            string[] properties = new string[] { "FreeMegabytes", "AvgDiskQueueLength" };
            Dictionary<string,string> values = GetWMIdata("Win32_PerfFormattedData_PerfDisk_LogicalDisk", properties, "Name", diskName);
            pf.DiskFree = Int32.Parse(values["FreeMegabytes"]);
            pf.DiskQueue = Int32.Parse(values["AvgDiskQueueLength"]);
        }




        // TODO: maybe need to be started in new Thread?
        // Runs executable provided by "proc" parameter, which also holds information how this process should be executed
        public static RunCompletedStatus StartProcess(RunProcess proc)
        {
            RunCompletedStatus status = new RunCompletedStatus();   // creates new object to return
            status.RunId = proc.RunId;

            Thread.Sleep(proc.Delay);

            Process newProcess = null;
            ProcessStartInfo execInfo = new ProcessStartInfo();
            execInfo.FileName = proc.Cmd;
            execInfo.Arguments = proc.Args;
            execInfo.CreateNoWindow = false;
            execInfo.UseShellExecute = false;
            execInfo.RedirectStandardError = true;
            if (proc.Hidden)
                execInfo.WindowStyle = ProcessWindowStyle.Hidden;
            
            try
            {
                newProcess = Process.Start(execInfo);

                string stderr = newProcess.StandardError.ReadToEnd();       //get error output

                newProcess.WaitForExit(proc.TimeOut);  // wait for process completion or timeout
                
                status.ExitCode = newProcess.ExitCode;
                if (status.ExitCode > 0)
                    status.ErrorMessage = stderr;
            }
            catch (Exception)
            {
                status.ErrorMessage = "ERROR: Unable to start \"" + proc.Cmd + "\"";
            }
            finally
            {
                if (newProcess != null)
                    newProcess.Close();
            }

            return status;
        }



        private static string GetWMIdata(string key, string property)
        {
            return GetWMIdata(key, property, null, null);
        }


        private static string GetWMIdata(string key, string property, string specificElementName, string specificElementValue)
        {
            string[] properties = new string[] { property };     // needed only for providing to another function

            Dictionary<string, string> values = GetWMIdata(key, properties);

            return values[property];
        }


        private static Dictionary<string, string> GetWMIdata(string key, string[] properties)
        {
            return GetWMIdata(key, properties, null, null);
        }


        private static Dictionary<string, string> GetWMIdata(string key, string[] properties, string specificElementName, string specificElementValue)
        {
            Dictionary<string, string> values = new Dictionary<string, string>();      //return value
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from " + key);
                foreach (ManagementObject element in searcher.Get())
                {
                    if (specificElementName == null || element[specificElementName].ToString() == specificElementValue)
                    {
                        foreach (string property in properties)
                        {
                            try
                            {
                                values.Add(property, element[property].ToString());
                            }
                            catch (Exception)
                            {
                                // just ignore exception bacause it some properties don't exist in all OS platforms (like OSArchitecture)
                                Logger.WriteStr(" WARNING: \"" + property + "\" does not exist in " + key);
                            }                         
                        }
                    }
                    
                }
            }
            catch (Exception ex)
            {
                //TODO:  throw new ArgumentException ?
                Logger.WriteStr(" ERROR: Cannot retrieve data from WMI key " + key + ". (Error: " + ex.Message + ")");
            }

            return values;
        }



    }
}
