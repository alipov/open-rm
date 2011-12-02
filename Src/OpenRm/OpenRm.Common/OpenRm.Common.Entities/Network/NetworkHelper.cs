using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace OpenRm.Common.Entities.Network
{
    public static class NetworkHelper
    {

        // Determinates if two IPs are on the same subnet. That meens that one can receive broadcasts of other (and of this subnet).
        public static bool IsOnSameNetwork(string ip1, string subnetMask1, string ip2, string subnetMask2)
        {
            // Loopback interfase, empty address cannot be used
            if (ip1 == "127.0.0.1" || ip2 == "127.0.0.1" || ip1 == "0.0.0.0" || ip1 == "127.0.0.1"
                || subnetMask1 == "" || subnetMask2 == "")
            {
                return false;
            }

            IPAddress ipAddress1 = IPAddress.Parse(ip1);
            IPAddress ipAddress2 = IPAddress.Parse(ip2);
            IPAddress subnetMaskAddress1 = IPAddress.Parse(subnetMask1);
            IPAddress subnetMaskAddress2 = IPAddress.Parse(subnetMask2);

            IPAddress networkAddress1 = GetNetworkAddress(ipAddress1, subnetMaskAddress1);
            IPAddress networkAddress2 = GetNetworkAddress(ipAddress2, subnetMaskAddress2);

            return networkAddress1.Equals(networkAddress2);
        }


        private static IPAddress GetNetworkAddress(IPAddress ip, IPAddress subnetMask)
        {
            byte[] ipAdressBytes = ip.GetAddressBytes();
            byte[] subnetMaskBytes = subnetMask.GetAddressBytes();

            byte[] networkAddress = new byte[ipAdressBytes.Length];
            for (int i = 0; i < networkAddress.Length; i++)
            {
                networkAddress[i] = (byte)(ipAdressBytes[i] & (subnetMaskBytes[i]));
            }
            return new IPAddress(networkAddress);
        }


    }
}
