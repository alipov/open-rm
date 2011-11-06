using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRm.Common.Entities;
using System.Management;
using System.Net.NetworkInformation;

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




        private static string GetWMIdata(string key, string property)
        {
            string value = "";       //return value
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("select " + property + " from " + key);
                foreach (ManagementObject element in searcher.Get())
                {
                    value = element[property].ToString();
                }
            }
            catch (Exception ex)
            {
                //TODO:  throw new ArgumentException ?
                Logger.WriteStr(" ERROR: Cannot retrieve " + property + " from WMI key " + key + ". (Error: " + ex.Message + ")");
            }

            return value;
        }



    }
}
