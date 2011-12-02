using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using OpenRm.Common.Entities.Network.Messages;

namespace OpenRm.Agent.Actions
{
    public static class WakeOnLanExecutor
    {
        //Sends Wake-On-Lan ("magic") packet to the specified MAC address
        public static ResponseBase Run(WakeOnLanRequest request)
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
                    packet[i * 6 + j] = byte.Parse(request.Mac.Substring(j * 2, 2), NumberStyles.HexNumber);

            try
            {
                // send the magic packet
                client.Send(packet, packet.Length);
                return new WakeOnLanResponse(true, request.RunId);
            }
            catch (Exception)
            {
                return new WakeOnLanResponse(false, request.RunId);
            }
            
        }

    }
}
