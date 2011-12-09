using System.Net;
using System.Net.NetworkInformation;

namespace OpenRm.Common.Entities.Network.Messages
{
    public class IpConfigRequest : RequestBase
    {
        public IPAddress IpAddress { get; set; }

        public override ResponseBase ExecuteRequest()
        {
            IpConfigResponse ipconf = new IpConfigResponse();
            ipconf.IpAddress = IpAddress.ToString();        // IP got from socket info

            foreach (NetworkInterface netInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (netInterface.OperationalStatus == OperationalStatus.Up)
                {
                    foreach (GatewayIPAddressInformation g in netInterface.GetIPProperties().GatewayAddresses)
                    {
                        if (g.Address.ToString() != "0.0.0.0")
                            ipconf.DefaultGateway = g.Address.ToString();
                    }

                    foreach (UnicastIPAddressInformation ip in netInterface.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.ToString() != IpAddress.ToString()) continue;    //match only interface that connection to server uses

                        if (ip.IPv4Mask != null)
                            ipconf.NetMask = ip.IPv4Mask.ToString();
                        else
                            ipconf.NetMask = "";        //Loopback has no Network Mask

                        ipconf.MAC = netInterface.GetPhysicalAddress().ToString();

                        break;
                    }

                }
            }
            return ipconf;
        }
    }
}
