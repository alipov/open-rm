using System.Net.NetworkInformation;
using OpenRm.Common.Entities.Network.Messages;

namespace OpenRm.Agent.Actions
{
    public static class IpConfigDataRetriever
    {
        public static ResponseBase Run(string sourceIP)
        {
            IpConfigData ipconf = new IpConfigData();
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

                    foreach (UnicastIPAddressInformation ip in netInterface.GetIPProperties().UnicastAddresses)
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
            return ipconf;
        }
    }
}
