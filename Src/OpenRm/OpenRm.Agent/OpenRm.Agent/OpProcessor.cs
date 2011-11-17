using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Win32;
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
                    {
                        if (ip.Address.ToString() != sourceIP) continue;    //match only interface that connection to server uses

                        if (ip.IPv4Mask != null)
                            ipconf.netMask = ip.IPv4Mask.ToString();
                        else
                            ipconf.netMask = "";        //Loopback has no Network Mask

                        ipconf.mac = netInterface.GetPhysicalAddress().ToString();

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


        public static List<string> GetInstalledPrograms()
        {
            var installedPrograms = new List<string>();

            // look in two registry locations: 1st - for 32-bit application, 2nd - for 64-bit applications
            var uninstallKey = new string[] { @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                                              @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall" };
            foreach (string uninstKey in uninstallKey)
            {
                using (RegistryKey socketkey = Registry.LocalMachine.OpenSubKey(uninstKey))
                {
                    if (socketkey != null)
                        foreach (string applKeyName in socketkey.GetSubKeyNames())
                            using (RegistryKey applKey = socketkey.OpenSubKey(applKeyName))
                            {
                                if (applKey != null && applKey.GetValue("DisplayName") != null)
                                    installedPrograms.Add(applKey.GetValue("DisplayName") + "  " + applKey.GetValue("DisplayVersion"));
                            }
                }
            }

            // Remove duplicates and sort in lexiographic order
            List<string> result = installedPrograms.Distinct().ToList();
            result.Sort();

            return result;
        }



        
        /* To get data: 
                var l = (List<string>)TraceRoute("8.8.8.8");
                foreach (string s in l)
                {
                    Console.WriteLine(s);    
                } 
         */
        public static RunCommonResponse ExecuteTraceRoute(RunTraceRoute tr)
        {
            var resultList = (List<string>)SendPingWithTtl(tr.Target, 1);     //start with minimum ttl in order to increase it recursively
            var resultString = "";
            foreach (string str in resultList)
            {
                resultString += str + "\n";
            }

            return new RunCommonResponse(tr.RunId, resultString);
        }

        public static RunCommonResponse ExecutePing(RunPing p)
        {
            var resultList = (List<string>)SendPingWithTtl(p.Target, 32);   //start with maximum ttl (32) in order to make just one ping
            var resultString = "";
            foreach (string str in resultList)
            {
                resultString += str + "\n";
            }
            return new RunCommonResponse(p.RunId, resultString);
        }

        private static IEnumerable<string> SendPingWithTtl(string target, int ttl)
        {
            const int timeout = 5000;          // 5 sec timeout
            const int maxTtl = 32;             // Max TTL
            var result = new List<string>();

            Ping ping = new Ping();
            PingOptions options = new PingOptions(ttl, true);
            byte[] buffer = Encoding.ASCII.GetBytes("abcdefghigklmnop");    // send some data

            try
            {
                PingReply reply = ping.Send(target, timeout, buffer, options);

                if (reply != null)
                {
                    if (reply.Status == IPStatus.Success)
                    {
                        result.Add(reply.Address.ToString() + "   " +
                            reply.RoundtripTime.ToString(NumberFormatInfo.CurrentInfo) + "ms");
                    }
                    else if (reply.Status == IPStatus.TtlExpired || reply.Status == IPStatus.TimedOut)
                    {
                        if (reply.Status == IPStatus.TtlExpired)
                        {
                            //add the currently returned address
                            result.Add(reply.Address.ToString());
                        }
                        else
                        {
                            result.Add(GetPingStatus(reply.Status));
                        }

                        if (ttl <= maxTtl)
                        {
                            //recurse to get the next address...
                            IEnumerable<string> tempResult = SendPingWithTtl(target, ttl + 1);
                            result.AddRange(tempResult);
                        }
                        else
                        {
                            result.Add("Has reached defined maximum TTL (" + maxTtl + ").");
                        }
                    }
                    else
                    {
                        result.Add(GetPingStatus(reply.Status));
                    }
                }
                else
                {
                    result.Add("Error occured while sending ping. Please see log for more info...");
                }

            }
            catch (PingException ex)
            {
                result.Add("Error occured while sending ping: " + ex.Message);
            }

            return result;
        }

        private static string GetPingStatus(IPStatus status)
        {
            switch (status)
            {
                case IPStatus.Success:
                    return "Success.";
                case IPStatus.DestinationHostUnreachable:
                    return "Destination host unreachable.";
                case IPStatus.DestinationNetworkUnreachable:
                    return "Destination network unreachable.";
                case IPStatus.DestinationPortUnreachable:
                    return "Destination port unreachable.";
                case IPStatus.DestinationProtocolUnreachable:
                    return "Destination protocol unreachable.";
                case IPStatus.PacketTooBig:
                    return "Packet too big.";
                case IPStatus.TtlExpired:
                    return "TTL expired.";
                case IPStatus.ParameterProblem:
                    return "Parameter problem.";
                case IPStatus.SourceQuench:
                    return "Source quench.";
                case IPStatus.TimedOut:
                    return "Request Timed out.";
                default:
                    return "Ping failed.";
            }
        }


        //Sends Wake-On-Lan ("magic") packet to the specified MAC address
        public static void WakeOnLan(string macAddr)
        {
            UdpClient client = new UdpClient();
            // it is typically sent as a UDP datagram to port 7 or 9
            client.Connect(IPAddress.Broadcast, 7);

            // The magic packet is a broadcast frame containing anywhere within its payload 6 bytes 
            //  of all 255 (FF FF FF FF FF FF in hexadecimal), followed by 16 repetitions of the target 
            //  computer's 6-byte MAC address:

            byte[] packet = new byte[17 * 6];

            for (int i = 0; i < 6; i++)
                packet[i] = 0xFF;

            for (int i = 1; i <= 16; i++)
                for (int j = 0; j < 6; j++)
                    packet[i * 6 + j] = byte.Parse(macAddr.Substring(j*2, 2), NumberStyles.HexNumber);
            
            // send the magic packet
            client.Send(packet, packet.Length);
        }



        // !TODO: maybe need to be started in new Thread?
        // Runs executable provided by "proc" parameter, which also holds information how this process should be executed
        public static RunCompletedStatus ExecuteProcess(RunProcess proc)
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
